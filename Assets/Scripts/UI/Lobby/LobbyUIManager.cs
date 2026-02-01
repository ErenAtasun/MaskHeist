using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaskHeist.Network;

namespace MaskHeist.UI.Lobby
{
    public class LobbyUIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_InputField ipInput;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private TextMeshProUGUI statusText;

        private MaskHeistNetworkManager manager;

        private void Start()
        {
            // NetworkManager singleton'ını bul
            manager = NetworkManager.singleton as MaskHeistNetworkManager;

            if (manager == null)
            {
                if (statusText) statusText.text = "Error: NetworkManager not found!";
                Debug.LogError("MaskHeistNetworkManager bulunamadı!");
                return;
            }

            // Önceki ismi yükle
            if (nameInput != null)
            {
                nameInput.text = PlayerPrefs.GetString("PlayerName", "Player" + Random.Range(100, 999));
            }

            // Buton dinleyicilerini ekle
            if (hostButton) hostButton.onClick.AddListener(OnHostClicked);
            if (joinButton) joinButton.onClick.AddListener(OnJoinClicked);
        }

        private bool wasConnecting = false;
        
        private void Update()
        {
            if (manager == null || statusText == null) return;
            
            // Bağlantı durumu takibi
            bool isConnecting = NetworkClient.active && !NetworkClient.isConnected;
            if (isConnecting && !wasConnecting)
            {
                Debug.Log($"[LobbyUI] NetworkClient.active={NetworkClient.active}, isConnected={NetworkClient.isConnected}");
                Debug.Log($"[LobbyUI] Transport: {manager.transport?.GetType().Name}");
            }
            wasConnecting = isConnecting;
            
            // Basit durum bilgilendirmesi
            if (NetworkServer.active && NetworkClient.active)
            {
                statusText.text = $"Host (Server + Client) Running... ({manager.networkAddress})";
            }
            else if (NetworkClient.isConnected)
            {
                statusText.text = $"Client Connected to {manager.networkAddress}";
            }
            else if (NetworkClient.active)
            {
                statusText.text = $"Connecting to {manager.networkAddress}...";
            }
        }

        private void OnHostClicked()
        {
            string playerName = nameInput.text;
            if (string.IsNullOrEmpty(playerName))
            {
                statusText.text = "Lütfen bir isim giriniz!";
                return;
            }

            PlayerPrefs.SetString("PlayerName", playerName);
            
            // Local test için host her zaman localhost olmalı
            manager.networkAddress = "localhost";
            manager.StartHost();
            statusText.text = "Starting Host (localhost)...";
        }

        private void OnJoinClicked()
        {
            string playerName = nameInput.text;
            if (string.IsNullOrEmpty(playerName))
            {
                statusText.text = "Lütfen bir isim giriniz!";
                return;
            }

            string ip = ipInput.text;
            if (string.IsNullOrEmpty(ip)) ip = "localhost";

            PlayerPrefs.SetString("PlayerName", playerName);
            
            // Debug: Bağlantı bilgilerini logla
            Debug.Log($"[LobbyUI] Bağlantı girişimi başlatılıyor...");
            Debug.Log($"[LobbyUI] Hedef IP: {ip}");
            
            if (manager.transport is kcp2k.KcpTransport kcp)
            {
                Debug.Log($"[LobbyUI] Transport: KCP, Port: {kcp.Port}");
                statusText.text = $"Connecting to {ip}:{kcp.Port}...";
            }
            else
            {
                statusText.text = $"Connecting to {ip}...";
            }
            
            manager.networkAddress = ip;
            manager.StartClient();
        }
    }
}
