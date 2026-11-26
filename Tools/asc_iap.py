"""App Store Connect'te satın alma ürünlerini kurar (idempotent: olanı atlar, eksik fiyat/çeviri/erişimi tamamlar).
Fiyat: taban ABD (USD) → diğer ülkeler Apple'ın kuruyla kendi para biriminde; Türkiye elle TL (Skus.TryPrices ile aynı).
Ortam: APPSTORE_KEY_ID, APPSTORE_ISSUER_ID, APPSTORE_P8."""
import os, re, time, json, sys, requests, jwt

BUNDLE = 'app.kilim.game'
API = 'https://api.appstoreconnect.apple.com'
# sku, tür, ad (tr), açıklama (tr), ad (en), açıklama (en), USD, TRY
P = [
    ('kilim.coins.1000', 'CONSUMABLE', '1.000 Altın', 'Bir avuç altın', '1,000 Coins', 'A handful of coins', '0.99', '49.99'),
    ('kilim.coins.2500', 'CONSUMABLE', '2.500 Altın', 'Bir kese altın', '2,500 Coins', 'A pouch of coins', '2.49', '119.99'),
    ('kilim.coins.6000', 'CONSUMABLE', '6.000 Altın', 'Bir sandık altın', '6,000 Coins', 'A chest of coins', '4.99', '229.99'),
    ('kilim.coins.14000', 'CONSUMABLE', '14.000 Altın', 'Büyük altın sandığı', '14,000 Coins', 'A big chest of coins', '9.99', '449.99'),
    ('kilim.coins.32000', 'CONSUMABLE', '32.000 Altın', 'Nine\'nin hazinesi', '32,000 Coins', 'Grandma\'s treasure', '19.99', '899.99'),
    ('kilim.lives', 'CONSUMABLE', 'Canları Doldur', 'Tüm canlar hemen dolar', 'Refill Lives', 'All lives refill instantly', '0.99', '49.99'),
    ('kilim.piggy', 'CONSUMABLE', 'Kumbarayı Kır', 'Kumbarada biriken altınlar', 'Break the Piggy Bank', 'Collect your saved coins', '2.99', '129.99'),
    ('kilim.goldpass', 'CONSUMABLE', 'Altın Yol', 'Bu sezonun altın ödülleri', 'Gold Path', 'This season\'s gold rewards', '4.99', '229.99'),
    ('kilim.noads', 'NON_CONSUMABLE', 'Reklamsız', 'Zorunlu reklamları kaldırır', 'No Ads', 'Removes forced ads', '4.99', '229.99'),
    ('kilim.starter', 'NON_CONSUMABLE', 'Başlangıç Paketi', 'Altın ve güçlendiriciler', 'Starter Pack', 'Coins and boosters', '1.99', '89.99'),
]

def token():
    key = os.environ['APPSTORE_P8'].strip()
    if 'BEGIN' not in key or '\n' not in key:
        body = re.sub(r'-----[^-]+-----|\s', '', key)
        key = '-----BEGIN PRIVATE KEY-----\n' + '\n'.join(body[i:i + 64] for i in range(0, len(body), 64)) + '\n-----END PRIVATE KEY-----\n'
    now = int(time.time())
    return jwt.encode({'iss': os.environ['APPSTORE_ISSUER_ID'], 'iat': now, 'exp': now + 1100, 'aud': 'appstoreconnect-v1'},
                      key, algorithm='ES256', headers={'kid': os.environ['APPSTORE_KEY_ID'], 'typ': 'JWT'})

S = requests.Session()
S.headers['Authorization'] = 'Bearer ' + token()

def call(m, path, body=None, ok=(200, 201, 204)):
    r = S.request(m, API + path, json=body)
    if r.status_code not in ok:
        print(f'!! {m} {path} → {r.status_code} {r.text[:600]}'); return None
    return r.json() if r.text else {}

def all_pages(path):
    out = []
    while path:
        j = call('GET', path)
        if not j: break
        out += j['data']; out_inc = j.get('included', [])
        all_pages.inc = getattr(all_pages, 'inc', []) + out_inc
        nxt = j.get('links', {}).get('next'); path = nxt.replace(API, '') if nxt else None
    return out

app = call('GET', f'/v1/apps?filter[bundleId]={BUNDLE}')['data'][0]['id']
have = {i['attributes']['productId']: i['id'] for i in all_pages(f'/v1/apps/{app}/inAppPurchasesV2?limit=200')}
territories = [t['id'] for t in all_pages('/v1/territories?limit=200')]
print('app', app, 'mevcut', list(have))

for sku, typ, ntr, dtr, nen, den, usd, tl in P:
    iid = have.get(sku)
    if not iid:
        j = call('POST', '/v2/inAppPurchases', {'data': {'type': 'inAppPurchases',
              'attributes': {'name': nen, 'productId': sku, 'inAppPurchaseType': typ, 'familySharable': False,
                             'reviewNote': 'Kilim in-game purchase. Test with any sandbox account from the Shop screen.'},
              'relationships': {'app': {'data': {'type': 'apps', 'id': app}}}}})
        if not j: continue
        iid = j['data']['id']; print('+ ürün', sku)
    # çeviriler
    locs = {l['attributes']['locale'] for l in all_pages(f'/v2/inAppPurchases/{iid}/inAppPurchaseLocalizations')}
    for loc, n, d in (('tr', ntr, dtr), ('en-US', nen, den)):
        if loc not in locs:
            call('POST', '/v1/inAppPurchaseLocalizations', {'data': {'type': 'inAppPurchaseLocalizations',
                 'attributes': {'locale': loc, 'name': n, 'description': d},
                 'relationships': {'inAppPurchaseV2': {'data': {'type': 'inAppPurchases', 'id': iid}}}}})
    # fiyat: USA taban + TUR elle
    pts = all_pages(f'/v2/inAppPurchases/{iid}/pricePoints?filter[territory]=USA,TUR&include=territory&limit=8000')
    def pick(terr, want):
        c = [p for p in pts if p['relationships']['territory']['data']['id'] == terr]
        return min(c, key=lambda p: abs(float(p['attributes']['customerPrice']) - float(want)))
    pu, pt = pick('USA', usd), pick('TUR', tl)
    call('POST', '/v1/inAppPurchasePriceSchedules', {
        'data': {'type': 'inAppPurchasePriceSchedules', 'relationships': {
            'inAppPurchase': {'data': {'type': 'inAppPurchases', 'id': iid}},
            'baseTerritory': {'data': {'type': 'territories', 'id': 'USA'}},
            'manualPrices': {'data': [{'type': 'inAppPurchasePrices', 'id': '${u}'}, {'type': 'inAppPurchasePrices', 'id': '${t}'}]}}},
        'included': [
            {'type': 'inAppPurchasePrices', 'id': '${u}', 'attributes': {'startDate': None},
             'relationships': {'inAppPurchasePricePoint': {'data': {'type': 'inAppPurchasePricePoints', 'id': pu['id']}}}},
            {'type': 'inAppPurchasePrices', 'id': '${t}', 'attributes': {'startDate': None},
             'relationships': {'inAppPurchasePricePoint': {'data': {'type': 'inAppPurchasePricePoints', 'id': pt['id']}}}}]})
    print(f'  fiyat {sku}: USA {pu["attributes"]["customerPrice"]} $, TUR {pt["attributes"]["customerPrice"]} ₺')
    # tüm ülkelerde satış
    call('POST', '/v1/inAppPurchaseAvailabilities', {'data': {'type': 'inAppPurchaseAvailabilities',
         'attributes': {'availableInNewTerritories': True},
         'relationships': {'inAppPurchase': {'data': {'type': 'inAppPurchases', 'id': iid}},
                           'availableTerritories': {'data': [{'type': 'territories', 'id': t} for t in territories]}}}}, ok=(200, 201, 409))
print('bitti')
