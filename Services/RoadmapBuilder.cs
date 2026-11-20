using ImproveYourCv.Models;

namespace ImproveYourCv.Services;

// Anket cevaplarından anında, kural tabanlı yol haritası üretir (API anahtarı gerekmez).
// Claude isterse MCP save_roadmap aracıyla bu planı daha kişisel bir planla değiştirebilir.
public static class RoadmapBuilder
{
    record Phase(string Title, int Weeks, string[] Topics, string Project);
    record Track(Phase[] Phases, string[] FirstWeek, RoadmapResource[] Resources);

    const string Backend = "Backend", Frontend = "Frontend", Mobile = "Mobil", Data = "Veri Bilimi ve Yapay Zekâ",
        Security = "Siber Güvenlik", DevOps = "DevOps ve Bulut", Game = "Oyun Geliştirme", Embedded = "Gömülü Sistemler ve IoT";

    // Bilinen teknoloji -> yakın olduğu alanlar (kararsız kullanıcıya alan önerirken)
    static readonly Dictionary<string, string[]> Affinity = new()
    {
        ["C"] = [Embedded], ["C++"] = [Game, Embedded], ["C#"] = [Backend, Game], ["Java"] = [Backend, Mobile],
        ["Python"] = [Data, DevOps, Security], ["JavaScript"] = [Frontend, Backend], ["SQL"] = [Backend, Data],
        ["HTML/CSS"] = [Frontend], ["Git"] = [DevOps], ["Linux"] = [DevOps, Security],
    };

    // Staj/iş hedefinde eşitlik olursa ilan sayısı yüksek alanlar öne alınır
    static readonly string[] JobMarketOrder = [Backend, Frontend, Data, Mobile, DevOps, Security, Game, Embedded];

    static RoadmapResource R(string title, string type) => new(title, type);

    static readonly Dictionary<string, Track> Tracks = new()
    {
        [Backend] = new(
            [
                new("C# ve nesne yönelimli programlama", 3, ["C#", "OOP ve SOLID", "LINQ", "async/await"], "Konsol tabanlı kütüphane takip uygulaması"),
                new("Veritabanı temelleri", 3, ["SQL", "İlişkisel modelleme", "Index ve sorgu performansı"], "Kütüphane uygulaması için veritabanı şeması"),
                new("ASP.NET Core Web API", 5, ["REST tasarımı", "EF Core", "Doğrulama ve hata yönetimi", "JWT ile kimlik doğrulama"], "Kimlik doğrulamalı not alma API'si"),
                new("Test ve kod kalitesi", 2, ["xUnit ile birim test", "Entegrasyon testi", "Git"], "API'ye testler ve GitHub Actions ile CI ekle"),
                new("Docker ve yayınlama", 3, ["Docker", "docker compose", "Bulutta yayınlama"], "API'yi container olarak yayınla"),
            ],
            [".NET SDK'yı ve Visual Studio'yu kur", "Microsoft Learn'de C# başlangıç yolunu başlat", "Kütüphane uygulamasının sınıflarını kağıda çiz"],
            [R("Microsoft Learn: C# ve ASP.NET Core", "Dokümantasyon"), R("roadmap.sh Backend", "Yol haritası"), R("PostgreSQL resmi dokümantasyonu", "Dokümantasyon")]),

        ["BackendJava"] = new(
            [
                new("Java ve nesne yönelimli programlama", 3, ["Java", "OOP ve SOLID", "Collections ve Stream API", "Maven/Gradle"], "Konsol tabanlı kütüphane takip uygulaması"),
                new("Veritabanı temelleri", 3, ["SQL", "İlişkisel modelleme", "Index ve sorgu performansı"], "Kütüphane uygulaması için veritabanı şeması"),
                new("Spring Boot ile REST API", 5, ["REST tasarımı", "Spring Data JPA", "Doğrulama ve hata yönetimi", "Spring Security ile JWT"], "Kimlik doğrulamalı not alma API'si"),
                new("Test ve kod kalitesi", 2, ["JUnit ile birim test", "Entegrasyon testi", "Git"], "API'ye testler ve GitHub Actions ile CI ekle"),
                new("Docker ve yayınlama", 3, ["Docker", "docker compose", "Bulutta yayınlama"], "API'yi container olarak yayınla"),
            ],
            ["JDK ve IntelliJ IDEA Community'yi kur", "Spring Initializr ile boş bir proje oluştur", "Kütüphane uygulamasının sınıflarını kağıda çiz"],
            [R("Spring resmi rehberleri (spring.io/guides)", "Dokümantasyon"), R("roadmap.sh Backend", "Yol haritası"), R("Effective Java — Joshua Bloch", "Kitap")]),

        [Frontend] = new(
            [
                new("Web temelleri", 3, ["HTML/CSS", "Flexbox ve Grid", "Responsive tasarım", "Erişilebilirlik temelleri"], "Kişisel tanıtım sayfası"),
                new("JavaScript", 4, ["JavaScript", "DOM", "fetch ve async/await", "ES modülleri"], "Açık bir API kullanan hava durumu uygulaması"),
                new("React", 5, ["Bileşenler ve props", "State ve hook'lar", "React Router", "Form yönetimi"], "Görev yönetimi uygulaması"),
                new("TypeScript ve araçlar", 3, ["TypeScript", "Vite", "Git", "Vitest ile test"], "Görev uygulamasını TypeScript'e taşı"),
                new("Yayınlama ve performans", 2, ["Lighthouse", "Lazy loading", "Vercel veya Netlify ile yayın"], "Projeyi yayınla ve Lighthouse skorunu iyileştir"),
            ],
            ["VS Code'u ve tarayıcı geliştirici araçlarını tanı", "MDN'de HTML ve CSS temel derslerine başla", "Tanıtım sayfanın taslağını çiz"],
            [R("MDN Web Docs", "Dokümantasyon"), R("javascript.info", "Eğitim"), R("react.dev resmi dokümantasyonu", "Dokümantasyon"), R("Frontend Mentor", "Pratik platformu")]),

        [Mobile] = new(
            [
                new("Kotlin temelleri", 3, ["Kotlin", "OOP", "Null güvenliği", "Coroutines"], "Konsol tabanlı alışveriş listesi"),
                new("Android ve Jetpack Compose", 5, ["Compose ile arayüz", "State yönetimi", "Navigation", "ViewModel"], "Not alma uygulaması"),
                new("Veri ve ağ", 4, ["Room veritabanı", "Retrofit ile REST", "Hata ve yükleme durumları"], "API'den veri çeken haber uygulaması"),
                new("Yayına hazırlık", 3, ["Git", "Birim test", "Google Play yayın süreci"], "Uygulamayı Play Store iç test kanalına yükle"),
            ],
            ["Android Studio'yu kur ve emülatörü çalıştır", "Android Basics with Compose kursuna başla", "Kotlin Playground'da ilk alıştırmaları çöz"],
            [R("Android Basics with Compose (developer.android.com)", "Kurs"), R("Kotlin resmi dokümantasyonu", "Dokümantasyon"), R("Now in Android örnek uygulaması", "Örnek kod")]),

        [Data] = new(
            [
                new("Python ve veri analizi", 4, ["Python", "NumPy", "pandas", "matplotlib ile görselleştirme"], "Açık bir veri setiyle keşifsel analiz"),
                new("İstatistik ve matematik", 3, ["Olasılık", "Hipotez testleri", "Lineer cebir temelleri"], "Analiz projesine istatistiksel testler ekle"),
                new("Makine öğrenmesi", 5, ["scikit-learn", "Regresyon ve sınıflandırma", "Model değerlendirme", "Özellik mühendisliği"], "Kaggle başlangıç yarışmasına katıl"),
                new("Derin öğrenme", 5, ["PyTorch", "Sinir ağları", "Görüntü ya da metin modelleri"], "Görüntü sınıflandırma modeli"),
                new("Veri ve yayınlama", 3, ["SQL", "Git", "Modeli FastAPI ile sunma"], "Modeli basit bir web API'siyle yayınla"),
            ],
            ["Python ve Jupyter Notebook'u kur", "Kaggle Learn'de Python kursuna başla", "İlgini çeken bir açık veri seti seç"],
            [R("Kaggle Learn", "Kurs"), R("Python for Data Analysis — Wes McKinney", "Kitap"), R("fast.ai Practical Deep Learning", "Kurs"), R("scikit-learn dokümantasyonu", "Dokümantasyon")]),

        [Security] = new(
            [
                new("Temeller", 4, ["Linux", "Ağ temelleri (TCP/IP, DNS, HTTP)", "Python"], "Python ile basit port tarayıcı"),
                new("Web güvenliği", 5, ["OWASP Top 10", "SQL injection ve XSS", "Burp Suite"], "PortSwigger Web Security Academy lab'larını tamamla"),
                new("Pratik ve CTF", 5, ["TryHackMe öğrenme yolları", "OverTheWire Bandit", "Kriptografi temelleri"], "Çözdüğün CTF'leri blogda anlat"),
                new("Savunma tarafı", 4, ["Log analizi", "SIEM temelleri", "Olay müdahalesi"], "Ev laboratuvarında log izleme kur"),
            ],
            ["Sanal makinede bir Linux dağıtımı kur", "OverTheWire Bandit'in ilk seviyelerini çöz", "TryHackMe'de ücretsiz bir başlangıç yoluna kaydol"],
            [R("TryHackMe", "Pratik platformu"), R("PortSwigger Web Security Academy", "Eğitim"), R("OWASP Top 10", "Dokümantasyon"), R("OverTheWire", "Pratik platformu")]),

        [DevOps] = new(
            [
                new("Linux ve betik yazma", 3, ["Linux", "Bash betikleri", "Git"], "Otomatik yedekleme betiği"),
                new("Container", 4, ["Docker", "docker compose", "Image optimizasyonu"], "Çok servisli bir uygulamayı compose ile çalıştır"),
                new("CI/CD", 3, ["GitHub Actions", "Otomatik test ve build", "Gizli bilgilerin yönetimi"], "Bir projeye CI/CD hattı kur"),
                new("Bulut", 5, ["Azure veya AWS temelleri", "Terraform", "İzleme ve loglama"], "Altyapıyı Terraform ile buluta kur"),
                new("Kubernetes", 4, ["Kubernetes temelleri", "Deployment ve Service", "Helm"], "Uygulamayı yerel Kubernetes'te çalıştır"),
            ],
            ["WSL ya da sanal makinede Linux kur", "Linux Journey'de ilk bölümleri bitir", "Docker Desktop'ı kur ve ilk container'ı çalıştır"],
            [R("Linux Journey", "Eğitim"), R("Docker resmi dokümantasyonu", "Dokümantasyon"), R("Microsoft Learn AZ-900", "Kurs"), R("Kubernetes resmi dokümantasyonu", "Dokümantasyon")]),

        [Game] = new(
            [
                new("C# temelleri", 3, ["C#", "OOP"], "Konsol tabanlı sayı tahmin oyunu"),
                new("Unity temelleri", 4, ["Sahne ve GameObject", "Fizik ve çarpışma", "Girdi sistemi"], "2D platform oyunu prototipi"),
                new("Oyun sistemleri", 5, ["Animasyon", "Arayüz (UI)", "Ses", "Kayıt sistemi"], "Prototipi menü ve skor sistemiyle tamamla"),
                new("Yayın", 3, ["Git", "Performans optimizasyonu", "itch.io'da yayın"], "Oyunu itch.io'da yayınla"),
            ],
            ["Unity Hub'ı ve Unity'yi kur", "Unity Learn'de Essentials yoluna başla", "Yapmak istediğin oyunun tek sayfalık tasarımını yaz"],
            [R("Unity Learn", "Kurs"), R("Game Programming Patterns — Robert Nystrom", "Kitap"), R("Godot resmi dokümantasyonu", "Dokümantasyon")]),

        [Embedded] = new(
            [
                new("C ve elektronik temelleri", 4, ["C", "Pointer ve bellek", "Temel devre bilgisi"], "Arduino ile LED ve buton projesi"),
                new("Mikrodenetleyiciler", 5, ["GPIO, ADC, PWM", "UART, I2C, SPI", "Kesmeler (interrupt)"], "Sensörden veri okuyup ekranda göster"),
                new("Gerçek zamanlı sistemler", 4, ["FreeRTOS", "Görevler ve kuyruklar", "Güç yönetimi"], "FreeRTOS ile çok görevli sensör istasyonu"),
                new("IoT", 4, ["ESP32 ve Wi-Fi", "MQTT", "Git"], "Sensör verisini MQTT ile bir panele gönder"),
            ],
            ["Bir Arduino ya da ESP32 başlangıç kiti edin", "Arduino IDE'yi kurup ilk LED projesini çalıştır", "C'de pointer konusunu tekrar et"],
            [R("Arduino resmi dokümantasyonu", "Dokümantasyon"), R("Making Embedded Systems — Elecia White", "Kitap"), R("FreeRTOS dokümantasyonu", "Dokümantasyon")]),
    };

    static readonly Phase CareerPhase = new("Portföy ve başvuru", 2,
        ["GitHub profili ve README'ler", "CV ve LinkedIn", "Mülakat ve algoritma pratiği"],
        "En iyi iki projeni öne çıkaran bir portföy sayfası");

    public static Roadmap Build(SurveyAnswers a)
    {
        var (field, reason) = ChooseField(a);
        var track = Tracks[field == Backend && a.Known.Contains("Java") && !a.Known.Contains("C#") ? "BackendJava" : field];
        var factor = a.Hours switch
        {
            "Haftada 1–3 saat" => 2.0,
            "Haftada 8–15 saat" => 0.6,
            "Haftada 15+ saat" => 0.4,
            _ => 1.0,
        };

        // Bilinen konular çıkarılır; tamamen bilinen aşama atlanır
        var phases = track.Phases.ToList();
        if (a.Goal is "Staj bulmak" or "İlk işimi bulmak") phases.Add(CareerPhase);
        var result = new List<RoadmapPhase>();
        var totalWeeks = 0;
        foreach (var p in phases)
        {
            var topics = p.Topics.Where(t => !a.Known.Contains(t)).ToList();
            if (topics.Count == 0) continue;
            var weeks = Math.Max(1, (int)Math.Ceiling(p.Weeks * factor * topics.Count / p.Topics.Length));
            totalWeeks += weeks;
            result.Add(new RoadmapPhase(p.Title, $"{weeks}-{weeks + 1} hafta", topics, p.Project));
        }

        var firstWeek = track.FirstWeek.ToList();
        if (!a.Known.Contains("Git")) firstWeek.Add("Git ve GitHub temellerini öğren, ilk reponu oluştur");

        var summary = $"{field} alanında {result.Count} aşamalı bir plan hazırladık. "
            + $"{a.Hours?.ToLowerInvariant()} çalışmaya göre toplam yaklaşık {totalWeeks}-{totalWeeks + result.Count} hafta sürer"
            + (a.Known.Count > 0 ? "; bildiğin konular plandan çıkarıldı." : ".");
        return new Roadmap(summary, field, reason, result, firstWeek, track.Resources.ToList());
    }

    static (string Field, string Reason) ChooseField(SurveyAnswers a)
    {
        var related = (string f) => a.Known.Where(k => Affinity.GetValueOrDefault(k, []).Contains(f)).ToList();
        if (a.Status == SurveyOptions.Status[0] && a.Fields.Count > 0)
        {
            var chosen = a.Fields[0];
            var rel = related(chosen);
            return (chosen, rel.Count > 0
                ? $"Bu alanı seçtin ve {string.Join(", ", rel)} bilgin bu alanda sana hızlı bir başlangıç sağlıyor."
                : "Bu alanı seçtin; plan temelden başlayıp adım adım ilerliyor.");
        }

        var candidates = a.Fields.Count > 0 ? a.Fields : [.. JobMarketOrder];
        var best = candidates
            .OrderByDescending(f => related(f).Count)
            .ThenBy(f => Array.IndexOf(JobMarketOrder, f))
            .First();
        var known = related(best);
        var why = known.Count > 0
            ? $"Bildiğin {string.Join(", ", known)} bu alanla doğrudan ilişkili, bu yüzden en hızlı ilerleyeceğin alan bu."
            : $"Başlangıç için iş ve staj ilanlarında en çok aranan alanlardan biri; temelleri diğer alanlara da kolayca aktarılır.";
        return (best, a.Fields.Count > 1 ? $"Seçtiğin alanlar arasında: {why}" : why);
    }
}
