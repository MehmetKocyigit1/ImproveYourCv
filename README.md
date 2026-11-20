# ImproveYourCv · DevRota

Bilgisayar mühendisliği öğrencileri için kişisel portföy ve öğrenme yol haritası uygulaması.

- **Portföy:** projeler, yetenekler (seviyelere göre) ve hedefler tek sayfada.
- **GitHub'dan ekle:** GitHub repolarını seçerek proje olarak içe aktarır; kullanılan diller yeteneklere işlenir.
- **Yol haritası anketi:** ziyaretçi 7 soruyu cevaplar (ilgi alanı, bildikleri, hedefi, ayırabileceği süre) ve kişisel öğrenme planını anında görür. Alanını seçmemiş kullanıcıya bildiklerine göre alan önerilir.
- **Hesaplar:** ziyaretçiler kayıt olup yol haritalarını hesaplarında saklayabilir, hesaplarını silebilir.
- **Yönetim:** yetenek, proje, hedef ve görevler `Admin` rolündeki hesapla düzenlenir.
- **Claude entegrasyonu (MCP):** Claude, MCP sunucusu üzerinden profili okuyup hedef/görev ekleyebilir, araştırma notu kaydedebilir ve anket cevaplarına daha kişisel yol haritası yazabilir.

## Teknolojiler

ASP.NET Core 9 MVC · Entity Framework Core 9 · SQL Server · ASP.NET Core Identity · Model Context Protocol C# SDK

## Kurulum

Gereksinimler: .NET 9 SDK ve SQL Server LocalDB (Visual Studio ile gelir).

```bash
git clone https://github.com/MehmetKocyigit1/ImproveYourCv.git
cd ImproveYourCv
dotnet run
```

Veritabanı (`(localdb)\MSSQLLocalDB` üzerinde `DevRota`) ilk açılışta migration'larla otomatik oluşturulur.

### Yönetici hesabı

Yönetici olacak e-postayı user-secrets ile ayarla, sonra sitede bu e-postayla kayıt ol:

```bash
dotnet user-secrets set "Admin:Email" "ornek@eposta.com"
```

### Ayarlar

| Ayar | Açıklama | Varsayılan |
|---|---|---|
| `ConnectionStrings:Default` | SQL Server bağlantısı | LocalDB |
| `Admin:Email` | Kayıt olunca `Admin` rolü alacak e-posta | — |
| `GitHub:Token` | GitHub API istek sınırını yükseltmek için (isteğe bağlı) | — |
| `Survey:HourlyLimitPerIp` | IP başına saatlik anket gönderimi | 3 |
| `Auth:HourlyLimitPerIp` | IP başına saatlik giriş/kayıt denemesi | 20 |
| `Profile:*` | Ana sayfadaki isim, unvan, tanıtım ve linkler | `appsettings.json` |

### Claude'a bağlama (MCP)

```bash
dotnet publish -c Release -o <klasör>
claude mcp add --scope user devrota -- dotnet "<klasör>/DevRota.dll" --mcp
```

MCP sunucusu `bin` yerine ayrı bir klasörden çalıştırılır; böylece Visual Studio derlerken dll kilitlenmez.

## Yayınlama (Azure App Service + Azure SQL)

1. Azure SQL veritabanı oluştur; bağlantı dizesini App Service'te `ConnectionStrings__Default` olarak ekle.
2. App Service uygulama ayarlarına `Admin__Email` ve `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` ekle (hız sınırının gerçek IP'yi görmesi için).
3. `dotnet publish -c Release` çıktısını yayınla; migration'lar ilk açılışta uygulanır.

## Lisans

MIT
