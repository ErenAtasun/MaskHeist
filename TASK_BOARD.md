# 🎭 MaskHeist - Geliştirme Takip Panosu (GDD Based)

Bu pano, GDD maddelerine göre detaylandırılmış ve rollere dağıtılmıştır.

## 👥 Ekip Rolleri
*   **Dev 1 (Sen):** Network, Core Systems, Backend Logic, Traps (Tuzaklar).
*   **Dev 2:** Gameplay, Character, UI, Score, Weapons.
*   **Designer:** Map, Level Design, Assets.

---

## 📅 Faz 1: Temel Altyapı & Harita (MVP Core)
**Hedef:** Oyuncuların lobiye girmesi, haritanın hazır olması ve oyunun başlaması.

### 🔧 Dev 1 (Network & Core & Traps)
- [x] **Network Manager:** `MaskHeistNetworkManager` (Room Server altyapısı).
- [x] **Lobby UI:** Oda oluşturma/katılma butonları ve listesi.
- [x] **Game Loop Temeli:** `GameFlowManager` (Faz geçişleri yazıldı, içi doldurulacak).
- [x] **Rol Dağıtımı:** `AssignRoles` fonksiyonu (1 Hider, 7 Seeker seçimi).
- [x] **Tuzak Sistemi (Temel):** `TrapBase`, `ProximityMine`, `LaserTrap` kodlandı.
- [ ] **Spawn Noktaları:** Haritadaki spawn noktalarını sunucuya tanıtma.

### 🎮 Dev 2 (Gameplay & UI)
- [ ] **Karakter Kontrolcüsü:** Yürüme, koşma, zıplama (FPS/TPS).
- [ ] **Score Manager:** Puan sistemi.
- [ ] **Weapon System:** Pompalı ateşleme mekaniği.
- [ ] **UI/HUD:** Skor ve Süre entegrasyonu.

### 🎨 Designer (Map & Assets)
- [ ] **Level Design:** Apartman haritası (Blockout).
- [ ] **Asset Toplama:** Duvar, zemin, mobilya paketleri.
- [ ] **Saklanma Noktaları:** 30-40 adet nokta (Küçük/Orta/Büyük) yerleşimi.

---

## 📅 Faz 2: Oynanış Mekanikleri (Game Loop)
**Hedef:** Saklanma, arama, eşya toplama ve tuzak kurma.

### 🔧 Dev 1 (Backend Logic)
- [ ] **Loot Pool Sistemi:** Rastgele 10 eşya seçimi ve haritaya dağıtılması.
- [ ] **Anti-Camp (Backend):** Eşya yakınlığı takibi (8m radius) ve ceza hesaplama.
- [ ] **Tuzak Entegrasyonu:** Oyuncuların tuzağı yere koyabilmesi (Interaction).
- [ ] **State Sync:** Süre ve faz durumunun tüm clientlarda aynı olması.

### 🎮 Dev 2 (Gameplay Features)
- [ ] **Interaction:** 'E' tuşu ile saklanma ve eşya alma.
- [ ] **Hider Mekanikleri:**
    - [ ] Tuzak token sistemi.
- [ ] **Seeker Mekanikleri:**
    - [ ] Maske yeteneği (Görünmezlik).
    - [ ] Eşya çalma (1.5 sn bar dolumu).

---

## 🚀 Başlangıç Durumu (Dev 1 için)

**Tamamlananlar:**
*   `MaskHeistNetworkManager.cs` (Lobi).
*   `GameFlowManager.cs` (Fazlar).
*   `AssignRoles` (Roller).
*   `LobbyUIManager.cs` (Lobi Arayüzü).
*   `TrapBase.cs`, `ProximityMine.cs`, `LaserTrap.cs` (Tuzak Kodları).

**Sıradaki Görevler (Öncelik Sırasına Göre):**
1.  **Loot Manager:** Eşyaların haritada çıkması sistemi (GDD Madde 5).
2.  **Tuzak Yerleştirme (Interaction):** Hider'ın elindeki tuzağı yere koyması.
