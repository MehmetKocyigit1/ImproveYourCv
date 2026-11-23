# ImproveYourCv

Bilgisayar mühendisleri ve öğrencileri için CV profili, eksik başlık analizi ve kişisel öğrenme yol haritası platformu.

- **CV profili:** her kullanıcı kendi profilini oluşturur: kişisel bilgiler ve özet, eğitim, iş deneyimi, stajlar, projeler, teknik ve sosyal yetenekler, sertifikalar, yabancı diller, yarışmalar ve başarılar, topluluk ve gönüllülük.
- **Profil doluluğu:** mühendis CV'lerinde beklenen başlıklardan hangilerinin eksik olduğunu yüzde ve öneri listesiyle gösterir.
- **GitHub'dan ekle:** GitHub repolarını seçerek proje olarak içe aktarır; kullanılan diller yeteneklere işlenir.
- **Yol haritası anketi:** 7 soru (ilgi alanı, bildikleri, hedefi, ayırabileceği süre) ve 8 alan için anında kişisel öğrenme planı. Alanını seçmemiş kullanıcıya bildiklerine göre alan önerilir.
- **Hedefler:** hedefleri görevlere bölme ve ilerleme takibi.
- **Hesaplar:** kayıt, giriş, hesap silme (hesapla birlikte profil verileri de silinir). Her kullanıcı yalnızca kendi verisini görür.
- **Claude entegrasyonu (MCP):** Claude, yönetici hesabının profilini okuyup güncelleyebilir, hedef ve araştırma notu ekleyebilir, anket cevaplarına daha kişisel yol haritası yazabilir.

## Teknolojiler

ASP.NET Core 9 MVC · Entity Framework Core 9 · SQL Server · ASP.NET Core Identity · Model Context Protocol C# SDK

## Kurulum

Gereksinimler: .NET 9 SDK ve SQL Server LocalDB (Visual Studio ile gelir).

```bash
git clone https://github.com/MehmetKocyigit1/ImproveYourCv.git
cd ImproveYourCv
dotnet run
```

Veritabanı (`(localdb)\MSSQLLocalDB` üzerinde `ImproveYourCv`) ilk açılışta migration'larla otomatik oluşturulur.

### Yönetici hesabı

`Admin:Email` ayarlanmamışsa siteye **ilk kayıt olan hesap** yönetici olur. Yayına almadan önce ya kendin kayıt ol ya da yönetici e-postasını ayarla:

```bash
dotnet user-secrets set "Admin:Email" "ornek@eposta.com"
```

### Ayarlar

| Ayar | Açıklama | Varsayılan |
|---|---|---|
| `ConnectionStrings:Default` | SQL Server bağlantısı | LocalDB |
| `Admin:Email` | Kayıt olunca `Admin` rolü alacak e-posta | ilk kayıt olan hesap |
| `GitHub:Token` | GitHub API istek sınırını yükseltmek için; tüm kullanıcılar ortak kullandığı için yayında önerilir | — |
| `Survey:HourlyLimitPerIp` | IP başına saatlik anket gönderimi | 3 |
| `Auth:HourlyLimitPerIp` | IP başına saatlik giriş/kayıt denemesi | 20 |

### Claude'a bağlama (MCP)

```bash
dotnet publish -c Release -o <klasör>
claude mcp add --scope user improveyourcv -- dotnet "<klasör>/ImproveYourCv.dll" --mcp
```

MCP sunucusu `bin` yerine ayrı bir klasörden çalıştırılır; böylece Visual Studio derlerken dll kilitlenmez.

## Yayınlama (Azure App Service + Azure SQL)

1. Azure SQL veritabanı oluştur; bağlantı dizesini App Service'te `ConnectionStrings__Default` olarak ekle.
2. App Service uygulama ayarlarına `Admin__Email` ve `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` ekle (hız sınırının gerçek IP'yi görmesi için).
3. `dotnet publish -c Release` çıktısını yayınla; migration'lar ilk açılışta uygulanır.

## Lisans

MIT
