// Gizlilik politikası (mağazalar, AdMob/UMP için kalıcı adres): /privacy  (TR + EN)
const CONTACT = 'oyunkilim@gmail.com';
const UPDATED = '26 Kasım 2026 / November 26, 2026';

const tr = `
<h1>Kilim – Gizlilik Politikası</h1>
<p class="m">Son güncelleme: ${UPDATED}</p>
<p>Bu politika, Kilim mobil oyununu ("Oyun") kullanırken hangi verilerin işlendiğini açıklar. Veri sorumlusu: Burak Soylu (Türkiye). İletişim: <a href="mailto:${CONTACT}">${CONTACT}</a>.</p>
<h2>1. Topladığımız veriler</h2>
<ul>
<li><b>Anonim oyuncu kimliği:</b> Hesap açmanız gerekmez. Firebase Anonymous Authentication ile rastgele bir kimlik oluşturulur; adınız, e-postanız veya telefonunuz alınmaz.</li>
<li><b>Oyun ilerlemesi:</b> bölüm, yıldız, coin/ilmek, ev dekorasyonu, ayarlar. Cihazınızda ve buluta yedek olarak sunucumuzda (Cloudflare) saklanır.</li>
<li><b>Satın almalar:</b> App Store / Google Play makbuzu yalnızca satın almayı doğrulamak ve iade/sahtecilik kontrolü için işlenir. Ödeme bilgilerinizi biz görmeyiz; ödemeyi Apple veya Google alır.</li>
<li><b>Oyun içi analitik:</b> bölüm başlama/bitirme, mağaza açma gibi olaylar; oyunu iyileştirmek için, anonim kimlikle.</li>
<li><b>Reklam verileri:</b> Google AdMob; cihazınızın reklam kimliği (IDFA/AAID), IP adresi, cihaz bilgisi. Ayrıntı aşağıda.</li>
<li><b>Haftalık yarış:</b> yarışa katılırsanız sıralamada oyun içinde seçilen takma ad ve puan diğer oyunculara görünür.</li>
</ul>
<h2>2. Reklamlar ve rıza</h2>
<p>Oyun, reklam göstermek için Google AdMob kullanır. AEA, Birleşik Krallık ve İsviçre'deki kullanıcılardan kişiselleştirilmiş reklam için Google'ın rıza formu (UMP) aracılığıyla izin istenir; iOS'ta takip izni (ATT) ayrıca sorulur. Rıza vermezseniz kişiselleştirilmemiş reklam gösterilir. Tercihinizi oyunda <i>Ayarlar › Gizlilik ve rıza</i> bölümünden değiştirebilirsiniz. Google'ın verileri nasıl kullandığı: <a href="https://policies.google.com/technologies/partner-sites">policies.google.com/technologies/partner-sites</a>.</p>
<p>18 yaş altı olduğunu belirten oyunculara kişiselleştirilmiş reklam gösterilmez, satış içerikli bildirim gönderilmez ve harcama uyarıları uygulanır.</p>
<h2>3. Bildirimler</h2>
<p>Bildirimler cihazınızda yerel olarak planlanır (can doldu, günün deseni vb.); bunun için sunucuya veri gönderilmez. Oyunda <i>Ayarlar › Bildirimler</i> veya telefon ayarlarından kapatabilirsiniz.</p>
<h2>4. Verileri kimlerle paylaşıyoruz</h2>
<p>Verilerinizi satmayız. Hizmet sağlayıcılar: Google (Firebase, AdMob), Apple ve Google (mağaza/ödeme), Cloudflare (sunucu barındırma). Veriler bu sağlayıcıların Türkiye dışındaki sunucularında işlenebilir.</p>
<h2>5. Saklama süresi</h2>
<p>Oyun verileri hesabınızı silene kadar saklanır. Silme talebinden sonra 30 gün içinde sunucudan kalıcı olarak kaldırılır; yasal zorunluluk gereği satın alma kayıtları ilgili mevzuatın öngördüğü süre boyunca tutulabilir.</p>
<h2 id="silme">6. Verilerinizi silme</h2>
<p>Oyunda <i>Ayarlar › Verilerimi sil</i> ile tüm oyun verileriniz sunucudan silinir. Oyuna erişemiyorsanız <a href="mailto:${CONTACT}">${CONTACT}</a> adresine "veri silme" konulu e-posta gönderin; Ayarlar sayfasının en altında yazan Oyuncu ID'nizi ekleyin.</p>
<h2>7. Haklarınız (KVKK / GDPR)</h2>
<p>Verilerinize erişme, düzeltme, silme, işlemeye itiraz etme ve taşınabilirlik haklarına sahipsiniz. Taleplerinizi <a href="mailto:${CONTACT}">${CONTACT}</a> adresine iletebilirsiniz; en geç 30 gün içinde yanıtlanır. Ayrıca Kişisel Verileri Koruma Kurumu'na veya yetkili veri koruma otoritesine şikâyette bulunabilirsiniz.</p>
<h2>8. Çocuklar</h2>
<p>Oyun 13 yaş altı çocuklara yönelik değildir. Oyun ilk açılışta yaş grubunu sorar; 13 yaş altı olduğunu bildiren kullanıcılardan bilerek kişisel veri toplamayız.</p>
<h2>9. Değişiklikler</h2>
<p>Bu politika güncellenirse bu sayfadaki tarih değişir; önemli değişiklikler oyun içinde duyurulur.</p>`;

const en = `
<h1>Kilim – Privacy Policy</h1>
<p class="m">Last updated: ${UPDATED}</p>
<p>This policy explains what data is processed when you play the Kilim mobile game (the "Game"). Controller: Burak Soylu (Türkiye). Contact: <a href="mailto:${CONTACT}">${CONTACT}</a>.</p>
<h2>1. Data we collect</h2>
<ul>
<li><b>Anonymous player ID:</b> no sign-up required. Firebase Anonymous Authentication creates a random ID; we do not collect your name, email or phone number.</li>
<li><b>Game progress:</b> levels, stars, coins, home decoration, settings — stored on your device and backed up on our server (Cloudflare).</li>
<li><b>Purchases:</b> App Store / Google Play receipts are processed only to verify purchases and handle refunds/fraud. We never see your payment details; Apple or Google process the payment.</li>
<li><b>In-game analytics:</b> events such as level start/finish, linked to the anonymous ID, to improve the Game.</li>
<li><b>Advertising data:</b> Google AdMob may process your device advertising ID (IDFA/AAID), IP address and device information. See below.</li>
<li><b>Weekly race:</b> if you join, your in-game nickname and score are visible to other players.</li>
</ul>
<h2>2. Ads and consent</h2>
<p>The Game uses Google AdMob. Users in the EEA, UK and Switzerland are asked for consent via Google's consent form (UMP); on iOS the App Tracking Transparency prompt is also shown. Without consent, non-personalised ads are shown. You can change your choice in <i>Settings › Privacy &amp; consent</i>. How Google uses data: <a href="https://policies.google.com/technologies/partner-sites">policies.google.com/technologies/partner-sites</a>.</p>
<p>Players who indicate they are under 18 receive no personalised ads and no sales-related notifications, and spending reminders apply.</p>
<h2>3. Notifications</h2>
<p>Notifications are scheduled locally on your device (lives refilled, daily pattern, etc.); no data is sent to our server for this. Turn them off in <i>Settings › Notifications</i> or in your phone settings.</p>
<h2>4. Sharing</h2>
<p>We do not sell your data. Service providers: Google (Firebase, AdMob), Apple and Google (stores/payments), Cloudflare (hosting). Data may be processed on servers outside Türkiye.</p>
<h2>5. Retention</h2>
<p>Game data is kept until you delete your account; after a deletion request it is permanently removed from our server within 30 days. Purchase records may be retained as required by law.</p>
<h2 id="delete">6. Deleting your data</h2>
<p>Use <i>Settings › Delete my data</i> in the Game to erase all game data from our server. If you can't access the Game, email <a href="mailto:${CONTACT}">${CONTACT}</a> with the subject "data deletion" and include the Player ID shown at the bottom of the Settings page.</p>
<h2>7. Your rights (GDPR / KVKK)</h2>
<p>You have the right to access, rectify, erase, object to processing and port your data. Send requests to <a href="mailto:${CONTACT}">${CONTACT}</a>; we respond within 30 days. You may also complain to your data protection authority.</p>
<h2>8. Children</h2>
<p>The Game is not directed at children under 13. It asks for an age group on first launch, and we do not knowingly collect personal data from users who indicate they are under 13.</p>
<h2>9. Changes</h2>
<p>If this policy changes, the date above is updated; significant changes are announced in the Game.</p>`;

export const privacyPage = (lang: string) => new Response(`<!doctype html><html lang="${lang === 'en' ? 'en' : 'tr'}"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1"><title>Kilim – ${lang === 'en' ? 'Privacy Policy' : 'Gizlilik Politikası'}</title>
<style>body{font:16px/1.6 system-ui,sans-serif;max-width:760px;margin:0 auto;padding:24px;color:#2b211b;background:#fbf6ee}
h1{color:#9c2f22}h2{margin-top:28px;font-size:19px}a{color:#9c2f22}.m{color:#7a6a5c}.l{float:right}</style></head><body>
<p class="l"><a href="?lang=tr">Türkçe</a> · <a href="?lang=en">English</a></p>${lang === 'en' ? en : tr}</body></html>`,
  { headers: { 'content-type': 'text/html; charset=utf-8', 'cache-control': 'public, max-age=3600' } });
