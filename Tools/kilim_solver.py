"""Kilim bölüm üretici + çözücü.
Kurallar: tahta (loom) = size x size, 'blocked' hücreler dışarıda. Kuyruk (queue) sırayla gelir,
tezgâhta (tray) aynı anda en fazla TRAY parça görünür. Oyuncu tezgâhtan bir parçayı
tahtada boş ve sığan bir yere koyar; tezgâh kuyruktan dolar. Döndürme yok (güçlendirici hariç).
Tüm parçalar yerleşince ve tahta tam dolunca KAZANIR. Tezgâhtaki hiçbir parça sığmazsa
TEZGÂH DOLDU (kayıp). Desen: her hücrenin bir motif rengi var; parçanın rengi o hücrenin
motif rengiyle eşleşirse 'desen hücresi'. Yıldız: 1★ bitir, 2★ desen ≥ %60, 3★ desen %100."""
import random, json, sys, functools
TRAY=4
def norm(cells):
    mr=min(r for r,c in cells); mc=min(c for r,c in cells)
    return tuple(sorted((r-mr,c-mc) for r,c in cells))
def rotations(shape):
    res=[];cur=list(shape)
    for _ in range(4):
        n=norm(cur)
        if n not in res: res.append(n)
        cur=[(c,-r) for r,c in cur]
    return res
def placements(shape,size,free):
    out=[];seen=set()
    for sh in rotations(shape):
      for dr in range(size):
        for dc in range(size):
            cells=[(r+dr,c+dc) for r,c in sh]
            if all((r,c) in free for r,c in cells):
                m=0
                for r,c in cells: m|=1<<(r*size+c)
                if m not in seen: seen.add(m); out.append((m,cells))
    return out
def build(level):
    size=level['size']; blocked={tuple(x) for x in level['blocked']}
    free={(r,c) for r in range(size) for c in range(size) if (r,c) not in blocked}
    full=0
    for r,c in free: full|=1<<(r*size+c)
    motif={(r,c):level['motif'][r][c] for (r,c) in free}
    pcs=[]
    for p in level['queue']:
        pl=placements([tuple(x) for x in p['shape']],size,free)
        pcs.append([(m,sum(1 for rc in cells if motif[rc]==p['color'])) for m,cells in pl])
    return full,pcs
def solve(level):
    """returns (solvable, best_motif_cells, total_cells)"""
    full,pcs=build(level); n=len(pcs)
    total=bin(full).count('1')
    sys.setrecursionlimit(10000)
    @functools.lru_cache(maxsize=None)
    def best(mask,nxt,tray):
        if not tray: return 0 if mask==full else -10**9
        res=-10**9
        for i,pi in enumerate(tray):
            nt=list(tray[:i]+tray[i+1:])
            nn=nxt
            if nn<n: nt.append(nn); nn+=1
            nt=tuple(sorted(nt))
            for m,sc in pcs[pi]:
                if m&mask: continue
                v=best(mask|m,nn,nt)
                if v>=0 and v+sc>res: res=v+sc
        return res
    t0=tuple(range(min(TRAY,n)))
    b=best(0,min(TRAY,n),t0)
    best.cache_clear()
    return b>=0, b, total
def _ok(mask,full,size,minrem):
    empty=full&~mask; seen=0
    while empty&~seen:
        b=(empty&~seen)&-(empty&~seen); stack=[b.bit_length()-1]; comp=0; seen|=b
        while stack:
            i=stack.pop(); comp+=1; r,c=divmod(i,size)
            for rr,cc in ((r+1,c),(r-1,c),(r,c+1),(r,c-1)):
                if 0<=rr<size and 0<=cc<size:
                    j=rr*size+cc; bit=1<<j
                    if empty&bit and not seen&bit: seen|=bit; stack.append(j)
        if comp<minrem: return False
    return True
def random_rollouts(level,k=300,rng=None):
    """'Dikkatli oyuncu' ajanı: rastgele seçer ama minimum kalan parçadan küçük boş ada bırakan hamleyi yapmaz."""
    rng=rng or random.Random(1)
    full,pcs=build(level); n=len(pcs); size=level['size']; wins=0
    sizes=[len(p['shape']) for p in level['queue']]
    for _ in range(k):
        mask=0; tray=list(range(min(TRAY,n))); nxt=len(tray)
        while tray:
            rem=[sizes[i] for i in tray]+sizes[nxt:]
            opts=[]
            for i,pi in enumerate(tray):
                rem2=rem[:i]+rem[i+1:]; mn=min(rem2) if rem2 else 1
                for m,_ in pcs[pi]:
                    if not m&mask and _ok(mask|m,full,size,mn): opts.append((i,m))
            if not opts: break
            i,m=rng.choice(opts); mask|=m; tray.pop(i)
            if nxt<n: tray.append(nxt); nxt+=1
        if not tray and mask==full: wins+=1
    return wins/k
def _old_random(level,k=300,rng=None):
    rng=rng or random.Random(1)
    full,pcs=build(level); n=len(pcs); wins=0
    for _ in range(k):
        mask=0; tray=list(range(min(TRAY,n))); nxt=len(tray)
        while tray:
            opts=[(i,m) for i,pi in enumerate(tray) for m,_ in pcs[pi] if not m&mask]
            if not opts: break
            i,m=rng.choice(opts); mask|=m; tray.pop(i)
            if nxt<n: tray.append(nxt); nxt+=1
        if not tray and mask==full: wins+=1
    return wins/k
COLORS=['K','L','S','Z','M']  # Kök boya kırmızı, Lacivert, Safran, Zeytin, Mürdüm
def gen(size,blocked,minp,maxp,ncol,rng):
    free=[(r,c) for r in range(size) for c in range(size) if (r,c) not in blocked]
    unassigned=set(free); pieces=[]
    while unassigned:
        start=min(unassigned) if rng.random()<0.5 else rng.choice(sorted(unassigned))
        target=rng.randint(minp,maxp); cells=[start]; unassigned.discard(start)
        while len(cells)<target:
            fr=[(r+dr,c+dc) for r,c in cells for dr,dc in ((1,0),(-1,0),(0,1),(0,-1)) if (r+dr,c+dc) in unassigned]
            if not fr: break
            x=rng.choice(fr); cells.append(x); unassigned.discard(x)
        pieces.append(cells)
    for p in [p for p in pieces if len(p)==1]:
        r,c=p[0]
        for q in pieces:
            if q is not p and len(q)<maxp+1 and any(abs(r-a)+abs(c-b)==1 for a,b in q):
                q.append((r,c)); pieces.remove(p); break
    if any(len(p)==1 for p in pieces): return None
    motif=[['' for _ in range(size)] for _ in range(size)]
    q=[]
    for p in pieces:
        col=rng.choice(COLORS[:ncol])
        for r,c in p: motif[r][c]=col
        q.append({'shape':[list(x) for x in norm(p)],'color':col,'_at':min(r*size+c for r,c in p)})
    rng.shuffle(q)
    return {'size':size,'blocked':[list(b) for b in sorted(blocked)],'motif':motif,'queue':q}
SIL={
 5:set(),
 6:{(0,0),(0,5),(5,0),(5,5)},
 7:{(0,0),(0,6),(6,0),(6,6),(3,3)},
}
if __name__=='__main__':
    # id: (size, minp,maxp, ncolors, (lo,hi) random-win-rate target)
    spec={1:(5,4,5,2,(0.85,1)),2:(5,3,5,2,(0.6,1)),3:(5,3,5,3,(0.5,0.9)),4:(6,3,5,3,(0.4,0.8)),5:(6,3,5,3,(0.2,0.5)),
          6:(6,3,5,3,(0.4,0.8)),7:(6,3,5,3,(0.3,0.6)),8:(6,3,5,4,(0.2,0.45)),9:(6,3,5,4,(0.15,0.35)),10:(6,3,5,4,(0.05,0.18)),
          11:(6,3,5,4,(0.3,0.6)),12:(7,3,5,4,(0.3,0.6)),13:(7,3,5,4,(0.2,0.45)),14:(7,3,5,4,(0.12,0.3)),15:(7,3,5,5,(0.05,0.15)),
          16:(7,3,5,5,(0.25,0.5)),17:(7,3,5,5,(0.15,0.35)),18:(7,3,5,5,(0.1,0.25)),19:(7,3,5,5,(0.06,0.18)),20:(7,3,5,5,(0.02,0.1))}
    rng=random.Random(20261112); out=[]
    for L,(size,mn,mx,nc,(lo,hi)) in spec.items():
        for att in range(4000):
            lv=gen(size,SIL[size],mn,mx,nc,rng)
            if not lv: continue
            wr=random_rollouts(lv,200,random.Random(att))
            if not(lo<=wr<=hi): continue
            ok,bm,tot=solve(lv)
            if not ok: continue
            lv.update(id=L,region=1,pieces=len(lv['queue']),cells=tot,randomWinRate=round(wr,3),bestMotif=bm,
                      stars={'2':int(tot*0.6+0.999),'3':tot},boss=L in (10,20),tutorial=L in (1,2,4))
            out.append(lv); print(L,size,'pcs',len(lv['queue']),'wr',round(wr,2),'cells',tot,'att',att,file=sys.stderr,flush=True)
            json.dump(out,open('/tmp/kilim/levels_r1.json','w'),ensure_ascii=False)
            break
        else: print('FAIL',L,file=sys.stderr,flush=True)

def player_rollouts(level,k=200,eps=0.25,rng=None):
    """'Ortalama oyuncu' ajanı: en kısıtlı boş hücreyi (en az seçenekli) doldurmaya çalışır,
    küçük boş ada bırakmaz; eps olasılıkla rastgele geçerli hamle yapar."""
    rng=rng or random.Random(7)
    full,pcs=build(level); n=len(pcs); size=level['size']; wins=0
    sizes=[len(p['shape']) for p in level['queue']]
    for _ in range(k):
        mask=0; tray=list(range(min(TRAY,n))); nxt=len(tray)
        while tray:
            rem=[sizes[i] for i in tray]+sizes[nxt:]
            opts=[]
            for i,pi in enumerate(tray):
                rem2=rem[:i]+rem[i+1:]; mn=min(rem2) if rem2 else 1
                for m,_ in pcs[pi]:
                    if not m&mask and _ok(mask|m,full,size,mn): opts.append((i,m))
            if not opts: break
            if rng.random()<eps: i,m=rng.choice(opts)
            else:
                empty=full&~mask; cnt={}
                for i2,m2 in opts:
                    b=m2
                    while b:
                        low=b&-b; cnt[low]=cnt.get(low,0)+1; b^=low
                cand=[(c,cell) for cell,c in cnt.items()]
                c0=min(c for c,_ in cand); cells=[cell for c,cell in cand if c==c0]
                cell=rng.choice(cells)
                i,m=rng.choice([(i2,m2) for i2,m2 in opts if m2&cell])
            mask|=m; tray.pop(i)
            if nxt<n: tray.append(nxt); nxt+=1
        if not tray and mask==full: wins+=1
    return wins/k

RECTS=[(1,2),(2,1),(1,3),(3,1),(2,2),(1,4),(4,1),(2,3),(3,2)]
def gen_rect(size,blocked,ncol,rng,allowed=RECTS):
    free={(r,c) for r in range(size) for c in range(size) if (r,c) not in blocked}
    un=set(free); pieces=[]
    while un:
        r0,c0=min(un); opts=[]
        for h,w in allowed:
            cells=[(r0+i,c0+j) for i in range(h) for j in range(w)]
            if all(x in un for x in cells): opts.append(cells)
        if not opts: return None
        cells=rng.choice(opts); pieces.append(cells); un-=set(cells)
    motif=[['' for _ in range(size)] for _ in range(size)]; q=[]
    for p in pieces:
        col=rng.choice(COLORS[:ncol])
        for r,c in p: motif[r][c]=col
        q.append({'shape':[list(x) for x in norm(p)],'color':col,'_at':min(r*size+c for r,c in p)})
    rng.shuffle(q)
    return {'size':size,'blocked':[list(b) for b in sorted(blocked)],'motif':motif,'queue':q}
def gen_mix(size,blocked,ncol,rng,rect_ratio):
    """Önce dikdörtgen tiling, sonra komşu parçaları rastgele birleştirerek L/T/S formları üret."""
    lv=gen_rect(size,blocked,ncol,rng)
    if not lv: return None
    # reconstruct pieces from motif is lossy; rebuild via merge on fresh tiling
    free={(r,c) for r in range(size) for c in range(size) if (r,c) not in blocked}
    un=set(free); pieces=[]
    while un:
        r0,c0=min(un); opts=[]
        for h,w in [(1,2),(2,1),(1,3),(3,1),(2,2)]:
            cells=[(r0+i,c0+j) for i in range(h) for j in range(w)]
            if all(x in un for x in cells): opts.append(cells)
        if not opts: return None
        cells=rng.choice(opts); pieces.append(cells); un-=set(cells)
    merged=True
    while merged:
        merged=False
        rng.shuffle(pieces)
        for a in pieces:
            if rng.random()<rect_ratio: continue
            for b in pieces:
                if a is b or len(a)+len(b)>5: continue
                if any(abs(r1-r2)+abs(c1-c2)==1 for r1,c1 in a for r2,c2 in b):
                    a.extend(b); pieces.remove(b); merged=True; break
            if merged: break
    motif=[['' for _ in range(size)] for _ in range(size)]; q=[]
    for p in pieces:
        col=rng.choice(COLORS[:ncol])
        for r,c in p: motif[r][c]=col
        q.append({'shape':[list(x) for x in norm(p)],'color':col,'_at':min(r*size+c for r,c in p)})
    rng.shuffle(q)
    return {'size':size,'blocked':[list(b) for b in sorted(blocked)],'motif':motif,'queue':q}
def run(spec,out_path,seed=20261112,k=100):
    import os
    out=json.load(open(out_path)) if os.path.exists(out_path) else []
    done={l['id'] for l in out}; rng=random.Random(seed+len(done))
    for L,(size,kind,ratio,nc,(lo,hi)) in spec.items():
        if L in done: continue
        for att in range(3000):
            lv=gen_rect(size,SIL[size],nc,rng) if kind=='rect' else gen_mix(size,SIL[size],nc,rng,ratio)
            if not lv: continue
            wr=player_rollouts(lv,k,rng=random.Random(att))
            if not(lo<=wr<=hi): continue
            ok=solve_fast(lv)
            if not ok: continue
            perf=solve_fast(lv,perfect=True)
            if not perf: continue
            tot=sum(len(p['shape']) for p in lv['queue'])
            lv.update(id=L,region=1,pieces=len(lv['queue']),cells=tot,playerWinRate=round(wr,2),perfectPossible=True,
                      stars={'2':int(tot*0.6+0.999),'3':tot},boss=L in (10,20),tutorial=L in (1,2,4))
            out.append(lv); out.sort(key=lambda x:x['id']); json.dump(out,open(out_path,'w'),ensure_ascii=False)
            print(L,size,kind,'pcs',len(lv['queue']),'wr',round(wr,2),'cells',tot,'att',att,file=sys.stderr,flush=True); break
        else: print('FAIL',L,file=sys.stderr,flush=True)

def solve_fast(level, perfect=False, limit=2_000_000):
    """Erken çıkışlı DFS. perfect=True: yalnızca tüm hücreleri motifle eşleşen yerleşimler (3★ mümkün mü?)."""
    full,pcs=build(level); n=len(pcs); size=level['size']
    sizes=[len(p['shape']) for p in level['queue']]
    if perfect: pcs=[[(m,sc) for m,sc in p if sc==sizes[i]] for i,p in enumerate(pcs)]
    seen=set(); cnt=[0]
    def dfs(mask,nxt,tray):
        cnt[0]+=1
        if cnt[0]>limit: raise TimeoutError
        if not tray: return mask==full
        key=(mask,nxt,tray)
        if key in seen: return False
        seen.add(key)
        # en kısıtlı boş hücre: onu kapsayan yerleşimleri dene
        empty=full&~mask; low=empty&-empty
        for i,pi in enumerate(tray):
            nt=list(tray[:i]+tray[i+1:]); nn=nxt
            if nn<n: nt.append(nn); nn+=1
            nt=tuple(sorted(nt))
            for m,_ in pcs[pi]:
                if m&mask or not m&low: continue
                if dfs(mask|m,nn,nt): return True
        return False
    t0=tuple(range(min(TRAY,n)))
    try: return dfs(0,len(t0),t0)
    except TimeoutError: return None

def solution_path(level, perfect=True):
    """3★ (tam desen) çözüm yolu: [(kuyruk_indeksi, [[r,c],...]), ...]"""
    size=level['size']; blocked={tuple(x) for x in level['blocked']}
    free={(r,c) for r in range(size) for c in range(size) if (r,c) not in blocked}
    full=0
    for r,c in free: full|=1<<(r*size+c)
    q=level['queue']; n=len(q)
    pcs=[]
    for p in q:
        pl=placements([tuple(x) for x in p['shape']],size,free)
        pcs.append([(m,cells) for m,cells in pl if (not perfect) or all(level['motif'][r][c]==p['color'] for r,c in cells)])
    seen=set()
    def dfs(mask,nxt,tray,path):
        if not tray: return path if mask==full else None
        key=(mask,nxt,tray)
        if key in seen: return None
        seen.add(key)
        empty=full&~mask; low=empty&-empty
        for i,pi in enumerate(tray):
            nt=list(tray[:i]+tray[i+1:]); nn=nxt
            if nn<n: nt.append(nn); nn+=1
            nt=tuple(sorted(nt))
            for m,cells in pcs[pi]:
                if m&mask or not m&low: continue
                r=dfs(mask|m,nn,nt,path+[(pi,[list(x) for x in cells])])
                if r: return r
        return None
    t0=tuple(range(min(TRAY,n)))
    return dfs(0,len(t0),t0,[])
