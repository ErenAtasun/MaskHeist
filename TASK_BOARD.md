# 🎭 MaskHeist - Geliştirme Takip Panosu (GDD Based)

Bu pano, GDD maddelerine göre detaylandırılmış ve rollere dağıtılmıştır.

## 👥 Ekip Rolleri
*   **Dev 1 (Sen):** Network, Core Systems, Backend Logic (Madde 2, 5, 6, 9)
*   **Dev 2:** Gameplay, Character, Traps, UI (Madde 3, 7, 8, 11)
*   **Designer:** Map, Level Design, Assets (Madde 4, 5-Assets)

---

## 📅 Faz 1: Temel Altyapı & Harita (MVP Core)
**Hedef:** Oyuncuların lobiye girmesi, haritanın hazır olması ve oyunun başlaması.

### 🔧 Dev 1 (Network & Core)
- [x] **Network Manager:** `MaskHeistNetworkManager` (Room Server altyapısı).
- [x] **Lobby UI:** Oda oluşturma/katılma butonları ve listesi.
- [x] **Game Loop Temeli:** `GameFlowManager` (Faz geçişleri yazıldı, içi doldurulacak).
- [x] **Rol Dağıtımı:** `AssignRoles` fonksiyonu (1 Hider, 7 Seeker seçimi).
- [ ] **Spawn Noktaları:** Haritadaki spawn noktalarını sunucuya tanıtma.

### 🎮 Dev 2 (Character & Control)
- [ ] **Karakter Modeli:** Şişko tatlı blob (Konsept hazır) -> Prefab yapımı.
- [ ] **Hareket Kodu:** `PlayerController` (Yürüme, Zıplama) + NetworkTransform.
- [ ] **Kamera:** 3rd Person kamera takibi.
- [ ] **Animasyonlar:** Idle, Walk/Run entegrasyonu.

### 🎨 Designer (Map & Assets)
- [ ] **Level Design:** Apartman haritası (Blockout).
- [ ] **Asset Toplama:** Duvar, zemin, mobilya paketleri.
- [ ] **Saklanma Noktaları:** 30-40 adet nokta (Küçük/Orta/Büyük) yerleşimi.
- [ ] **NavMesh:** Yapay zeka veya click-to-move gerekirse (Şu an opsiyonel).

---

## 📅 Faz 2: Oynanış Mekanikleri (Game Loop)
**Hedef:** Saklanma, arama, eşya toplama ve tuzak kurma.

### 🔧 Dev 1 (Backend Logic)
- [ ] **Loot Pool Sistemi:** Rastgele 10 eşya seçimi ve haritaya dağıtılması.
- [ ] **Anti-Camp (Backend):** Eşya yakınlığı takibi (8m radius) ve ceza hesaplama.
- [ ] **Skor Sistemi:** Puanların sunucuda tutulması ve hesaplanması.
- [ ] **State Sync:** Süre ve faz durumunun tüm clientlarda aynı olması.

### 🎮 Dev 2 (Gameplay Features)
- [ ] **Interaction:** 'E' tuşu ile saklanma ve eşya alma.
- [ ] **Hider Mekanikleri:**
    - [ ] Tuzak sistemi (Freeze Pad, Lazer Teli).
    - [ ] Tuzak token sistemi.
- [ ] **Seeker Mekanikleri:**
    - [ ] Maske yeteneği (Görünmezlik).
    - [ ] Eşya çalma (1.5 sn bar dolumu).
- [ ] **UI/HUD:**
    - [ ] Rol göstergesi, Süre, Yetenek Cooldown'ları.

---

## 📅 Faz 3: Cila & Final (Polish)
**Hedef:** GDD tam uyumluluk, görsel/ses efektleri ve bug temizliği.

### 🔧 Dev 1 (Finalize)
- [ ] **Disconnect Handling:** Kopan oyuncuyu yönetme.
- [ ] **Host Migration:** (Opsiyonel) Sunucu düşerse aktarma.
- [ ] **Match End:** Maç sonu verilerinin işlenmesi.

### 🎮 Dev 2 (UX & Feedback)
- [ ] **Efektler:** Tuzak patlama, görünmezlik efekti.
- [ ] **Sesler:** Adım sesleri (Görünmezken artan ses), ambiyans.
- [ ] **Menüler:** Ana menü, Ayarlar, Pause menüsü.

### 🎨 Designer (Atmosphere)
- [ ] **Işıklandırma:** Bake işlemleri.
- [ ] **Detaylandırma:** Dekoratif objeler.
- [ ] **Collision:** Hatalı çarpışmaların düzeltilmesi.

---

## 🚀 Başlangıç Durumu (Dev 1 için)

**Tamamlananlar:**
*   `MaskHeistNetworkManager.cs` oluşturuldu (Lobi altyapısı var).
*   `GameFlowManager.cs` oluşturuldu (Faz yapısı kurulu).
*   `AssignRoles` mantığı yazıldı (`MaskHeistGamePlayer` sınıfı ile).

**Sıradaki Görevler (Öncelik Sırasına Göre):**
1.  **Loot Manager:** Eşyaların haritada çıkması sistemi (GDD Madde 5).
2.  **Lobby Sahnesi Kurulumu:** Unity Editör'de LobbyUIManager'ı Canvas'a bağlama.
