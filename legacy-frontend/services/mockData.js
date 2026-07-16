const demoUsers = {
  "hr-admin": {
    id: "u-hr-001",
    name: "İK Yöneticisi",
    email: "ik@hrmaster.local",
    role: "hr-admin",
    roleLabel: "İK Admin",
    initials: "İK",
  },
  manager: {
    id: "u-mgr-001",
    name: "Ece Arslan",
    email: "ece.arslan@hrmaster.local",
    role: "manager",
    roleLabel: "Yönetici",
    initials: "EA",
  },
  employee: {
    id: "u-emp-001",
    name: "Ahmet Yılmaz",
    email: "ahmet.yilmaz@hrmaster.local",
    role: "employee",
    roleLabel: "Çalışan",
    initials: "AY",
  },
};

// Personel dizini: personel listesi ve global arama için tek kaynak.
const personnelDirectory = [
  { id: 1, name: "Ahmet Yılmaz", title: "Senior Developer", tc: "123*****", dept: "Yazılım", status: "active", initials: "AY", start: "12.03.2022" },
  { id: 2, name: "Ayşe Demir", title: "İK Uzmanı", tc: "456*****", dept: "İnsan Kaynakları", status: "active", initials: "AD", start: "04.09.2021" },
  { id: 3, name: "Selin Koç", title: "UI Designer", tc: "789*****", dept: "Tasarım", status: "passive", initials: "SK", start: "17.01.2023" },
];

const getDirectory = () => [...personnelDirectory];

const globalActions = [
  {
    id: "act-001",
    title: "Yazılım ekibinde tükenmişlik sinyali için kapasite görüşmesi planla",
    source: "Risk Merkezi",
    sourceRoute: "burnout-risk",
    owner: "Ece Arslan",
    due: "Bugün",
    priority: "high",
    status: "open",
    action: "Ekip lideriyle 30 dakikalık kapasite kontrolü oluştur.",
  },
  {
    id: "act-002",
    title: "Bordroda eksik puantajı tamamla",
    source: "Bordro",
    sourceRoute: "payroll",
    owner: "İK Operasyon",
    due: "Bugün",
    priority: "high",
    status: "open",
    action: "Mert Can için nöbet ve fazla mesai kapanışını kontrol et.",
  },
  {
    id: "act-003",
    title: "KVKK eklerini gün sonuna kadar kapat",
    source: "Uyum",
    sourceRoute: "compliance-risk",
    owner: "İK Operasyon",
    due: "Bugün",
    priority: "high",
    status: "open",
    action: "Eksik 4 kayıt için sorumlu atamasını netleştir.",
  },
  {
    id: "act-004",
    title: "Çalışan nabzı düşen ekip için takip anketi planla",
    source: "Çalışan Nabzı",
    sourceRoute: "employee-voice",
    owner: "İK Business Partner",
    due: "Bu hafta",
    priority: "medium",
    status: "week",
    action: "Yazılım ve Satış ekiplerinde kısa takip anketi yayınla.",
  },
  {
    id: "act-005",
    title: "İzin onayları ve ekip yokluk takvimini kontrol et",
    source: "İzin / Mesai",
    sourceRoute: "leaves",
    owner: "Yönetici",
    due: "Bu hafta",
    priority: "medium",
    status: "week",
    action: "Onay bekleyen talepleri ekip kapasitesiyle birlikte değerlendir.",
  },
  {
    id: "act-006",
    title: "Bordro ayarları güncellendi ve kontrol edildi",
    source: "Bordro",
    sourceRoute: "payroll",
    owner: "İK Yöneticisi",
    due: "Tamamlandı",
    priority: "low",
    status: "done",
    action: "Fazla mesai çarpanı ve SGK parametreleri kontrol edildi.",
  },
];

const auditLogs = [
  { id: "aud-001", actor: "İK Yöneticisi", action: "Kullanıcı giriş yaptı", module: "Auth", time: "Bugün 09:02", detail: "Demo session oluşturuldu." },
  { id: "aud-002", actor: "İK Yöneticisi", action: "Bordro ayarı güncellendi", module: "Bordro", time: "Bugün 09:18", detail: "Fazla mesai çarpanı 1.5 olarak kaydedildi." },
  { id: "aud-003", actor: "İK Operasyon", action: "Bordro ön hesabı çalıştırıldı", module: "Bordro", time: "Bugün 09:26", detail: "Nisan 2026 dönemi için 5 çalışan hesaplandı." },
  { id: "aud-004", actor: "İK Operasyon", action: "Uyum evrak durumu değişti", module: "Uyum", time: "Dün 17:42", detail: "KVKK açık rıza eki 'Eksik' durumundan 'İncelemede' durumuna alındı." },
  { id: "aud-005", actor: "Ece Arslan", action: "Risk aksiyonu onaya gönderildi", module: "Risk Merkezi", time: "Dün 14:10", detail: "Tükenmişlik sinyali için kapasite görüşmesi aksiyonu açıldı." },
];

const getMockActions = () => [...globalActions];
const getMockAuditLogs = () => [...auditLogs];
const getOpenActionCount = () => globalActions.filter((item) => item.status !== "done").length;
