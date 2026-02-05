using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace BobsPetroleum.Utilities
{
    /// <summary>
    /// DEBUG CONSOLE - Test your game faster!
    /// Press ~ (tilde) to open, type commands for cheats.
    ///
    /// COMMANDS:
    /// - god: Toggle god mode (invincible)
    /// - money [amount]: Add money
    /// - hamburger [amount]: Add hamburgers
    /// - heal: Full health
    /// - kill: Die
    /// - spawn customer: Spawn a customer
    /// - spawn [item]: Spawn an item
    /// - teleport [location]: Teleport to location
    /// - time [hour]: Set time of day
    /// - speed [multiplier]: Game speed
    /// - noclip: Toggle fly mode
    /// - bob heal: Heal Bob
    /// - bob damage [amount]: Damage Bob
    /// - win: Win the game
    /// - help: Show all commands
    /// </summary>
    public class DebugConsole : MonoBehaviour
    {
        public static DebugConsole Instance { get; private set; }

        [Header("=== TOGGLE ===")]
        [Tooltip("Key to open console")]
        public KeyCode toggleKey = KeyCode.BackQuote;

        [Tooltip("Is console open")]
        public bool isOpen = false;

        [Tooltip("Enable in builds?")]
        public bool enableInBuilds = false;

        [Header("=== UI REFERENCES ===")]
        [Tooltip("Console panel")]
        public GameObject consolePanel;

        [Tooltip("Input field")]
        public TMP_InputField inputField;

        [Tooltip("Output text")]
        public TMP_Text outputText;

        [Tooltip("Scroll rect for output")]
        public ScrollRect scrollRect;

        [Header("=== SETTINGS ===")]
        [Tooltip("Max output lines")]
        public int maxOutputLines = 100;

        [Tooltip("Command history size")]
        public int historySize = 20;

        // State
        private List<string> outputLines = new List<string>();
        private List<string> commandHistory = new List<string>();
        private int historyIndex = -1;

        // Cheat states
        private bool godMode = false;
        private bool noclipMode = false;
        private float originalTimeScale = 1f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Disable in builds if not allowed
            #if !UNITY_EDITOR
            if (!enableInBuilds)
            {
                gameObject.SetActive(false);
                return;
            }
            #endif
        }

        private void Start()
        {
            if (consolePanel == null)
            {
                CreateConsoleUI();
            }

            CloseConsole();
            Log("Debug Console initialized. Press ~ to open.");
            Log("Type 'help' for commands.");
        }

        private void Update()
        {
            // Toggle console
            if (Input.GetKeyDown(toggleKey))
            {
                if (isOpen) CloseConsole();
                else OpenConsole();
            }

            if (!isOpen) return;

            // Command history navigation
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                NavigateHistory(-1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                NavigateHistory(1);
            }

            // Submit command
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SubmitCommand();
            }

            // Update god mode
            if (godMode)
            {
                var player = Player.PlayerController.Instance;
                if (player != null)
                {
                    var health = player.GetComponent<Player.PlayerHealth>();
                    if (health != null)
                    {
                        health.SetHealth(health.maxHealth);
                    }
                }
            }
        }

        #region Console UI

        private void CreateConsoleUI()
        {
            // Create canvas
            GameObject canvasObj = new GameObject("DebugConsoleCanvas");
            canvasObj.transform.SetParent(transform);
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Console panel
            consolePanel = new GameObject("ConsolePanel");
            consolePanel.transform.SetParent(canvasObj.transform, false);
            var panelRect = consolePanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0.5f);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImg = consolePanel.AddComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0.9f);

            // Output area
            GameObject outputArea = new GameObject("OutputArea");
            outputArea.transform.SetParent(consolePanel.transform, false);
            var outputRect = outputArea.AddComponent<RectTransform>();
            outputRect.anchorMin = new Vector2(0, 0.1f);
            outputRect.anchorMax = Vector2.one;
            outputRect.offsetMin = new Vector2(10, 0);
            outputRect.offsetMax = new Vector2(-10, -10);

            scrollRect = outputArea.AddComponent<ScrollRect>();

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(outputArea.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            outputText = content.AddComponent<TextMeshProUGUI>();
            outputText.fontSize = 14;
            outputText.color = Color.white;

            scrollRect.content = contentRect;
            scrollRect.viewport = outputRect;

            // Input field
            GameObject inputObj = new GameObject("InputField");
            inputObj.transform.SetParent(consolePanel.transform, false);
            var inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = new Vector2(1, 0.1f);
            inputRect.offsetMin = new Vector2(10, 5);
            inputRect.offsetMax = new Vector2(-10, -5);

            var inputImg = inputObj.AddComponent<Image>();
            inputImg.color = new Color(0.2f, 0.2f, 0.2f);

            inputField = inputObj.AddComponent<TMP_InputField>();

            // Input text area
            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputObj.transform, false);
            var textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(5, 0);
            textAreaRect.offsetMax = new Vector2(-5, 0);

            var inputText = textArea.AddComponent<TextMeshProUGUI>();
            inputText.fontSize = 16;
            inputText.color = Color.white;

            inputField.textComponent = inputText;
            inputField.textViewport = textAreaRect;
        }

        public void OpenConsole()
        {
            isOpen = true;
            if (consolePanel != null)
            {
                consolePanel.SetActive(true);
            }

            if (inputField != null)
            {
                inputField.text = "";
                inputField.ActivateInputField();
            }

            // Don't pause - allow commands while playing
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseConsole()
        {
            isOpen = false;
            if (consolePanel != null)
            {
                consolePanel.SetActive(false);
            }

            // Restore cursor based on game state
            var gameState = Core.GameStateManager.Instance;
            if (gameState != null && gameState.IsPlaying)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        #endregion

        #region Command Processing

        private void SubmitCommand()
        {
            if (inputField == null) return;

            string command = inputField.text.Trim();
            if (string.IsNullOrEmpty(command)) return;

            // Add to history
            commandHistory.Insert(0, command);
            if (commandHistory.Count > historySize)
            {
                commandHistory.RemoveAt(commandHistory.Count - 1);
            }
            historyIndex = -1;

            // Log command
            Log($"> {command}", Color.cyan);

            // Process
            ProcessCommand(command);

            // Clear input
            inputField.text = "";
            inputField.ActivateInputField();
        }

        private void ProcessCommand(string command)
        {
            string[] parts = command.ToLower().Split(' ');
            string cmd = parts[0];
            string[] args = parts.Skip(1).ToArray();

            switch (cmd)
            {
                case "help":
                    ShowHelp();
                    break;

                case "god":
                    ToggleGodMode();
                    break;

                case "noclip":
                    ToggleNoclip();
                    break;

                case "money":
                    AddMoney(args);
                    break;

                case "hamburger":
                case "hamburgers":
                case "burger":
                    AddHamburgers(args);
                    break;

                case "heal":
                    HealPlayer();
                    break;

                case "kill":
                    KillPlayer();
                    break;

                case "spawn":
                    SpawnEntity(args);
                    break;

                case "teleport":
                case "tp":
                    Teleport(args);
                    break;

                case "time":
                    SetTime(args);
                    break;

                case "speed":
                    SetGameSpeed(args);
                    break;

                case "bob":
                    BobCommands(args);
                    break;

                case "win":
                    WinGame();
                    break;

                case "lose":
                    LoseGame();
                    break;

                case "clear":
                    ClearOutput();
                    break;

                case "fps":
                    ToggleFPS();
                    break;

                case "customer":
                    SpawnCustomer();
                    break;

                case "items":
                    ListItems();
                    break;

                case "locations":
                    ListLocations();
                    break;

                default:
                    Log($"Unknown command: {cmd}. Type 'help' for commands.", Color.red);
                    break;
            }
        }

        private void NavigateHistory(int direction)
        {
            if (commandHistory.Count == 0) return;

            historyIndex += direction;
            historyIndex = Mathf.Clamp(historyIndex, -1, commandHistory.Count - 1);

            if (historyIndex >= 0 && inputField != null)
            {
                inputField.text = commandHistory[historyIndex];
                inputField.caretPosition = inputField.text.Length;
            }
        }

        #endregion

        #region Commands

        private void ShowHelp()
        {
            Log("=== DEBUG CONSOLE COMMANDS ===", Color.yellow);
            Log("god - Toggle invincibility");
            Log("noclip - Toggle fly mode");
            Log("money [amount] - Add money (default: 1000)");
            Log("hamburger [amount] - Add hamburgers (default: 5)");
            Log("heal - Full health");
            Log("kill - Die instantly");
            Log("spawn customer - Spawn a customer");
            Log("spawn [item] - Spawn an item");
            Log("teleport [location] - Teleport (bob, shop, spawn)");
            Log("time [hour] - Set time (0-24)");
            Log("speed [multiplier] - Game speed (0.1-10)");
            Log("bob heal - Heal Bob fully");
            Log("bob damage [amount] - Damage Bob");
            Log("win - Win the game");
            Log("lose - Lose the game");
            Log("fps - Toggle FPS display");
            Log("clear - Clear console");
            Log("items - List spawnable items");
            Log("locations - List teleport locations");
        }

        private void ToggleGodMode()
        {
            godMode = !godMode;
            Log($"God mode: {(godMode ? "ON" : "OFF")}", godMode ? Color.green : Color.red);
        }

        private void ToggleNoclip()
        {
            noclipMode = !noclipMode;
            var player = Player.PlayerController.Instance;
            if (player != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.enabled = !noclipMode;
                }

                // Enable/disable gravity in player controller
                // This would need a property in PlayerController
            }
            Log($"Noclip mode: {(noclipMode ? "ON" : "OFF")}", noclipMode ? Color.green : Color.red);
        }

        private void AddMoney(string[] args)
        {
            int amount = 1000;
            if (args.Length > 0) int.TryParse(args[0], out amount);

            var player = FindObjectOfType<Player.PlayerInventory>();
            if (player != null)
            {
                player.AddMoney(amount);
                Log($"Added ${amount}. Total: ${player.Money}", Color.green);
            }
            else
            {
                Log("No player found!", Color.red);
            }
        }

        private void AddHamburgers(string[] args)
        {
            int amount = 5;
            if (args.Length > 0) int.TryParse(args[0], out amount);

            var player = FindObjectOfType<Player.PlayerInventory>();
            if (player != null)
            {
                for (int i = 0; i < amount; i++)
                {
                    player.AddHamburger();
                }
                Log($"Added {amount} hamburgers. Total: {player.Hamburgers}", Color.green);
            }
            else
            {
                Log("No player found!", Color.red);
            }
        }

        private void HealPlayer()
        {
            var player = FindObjectOfType<Player.PlayerHealth>();
            if (player != null)
            {
                player.Heal(player.maxHealth);
                Log("Player healed!", Color.green);
            }
            else
            {
                Log("No player found!", Color.red);
            }
        }

        private void KillPlayer()
        {
            var player = FindObjectOfType<Player.PlayerHealth>();
            if (player != null)
            {
                player.TakeDamage(9999);
                Log("Player killed!", Color.red);
            }
        }

        private void SpawnEntity(string[] args)
        {
            if (args.Length == 0)
            {
                Log("Usage: spawn [entity]. Use 'items' to see list.", Color.yellow);
                return;
            }

            string entity = string.Join(" ", args).ToLower();

            if (entity == "customer")
            {
                SpawnCustomer();
                return;
            }

            // Try to spawn as prefab
            Log($"Spawn system not fully implemented. Try: spawn customer", Color.yellow);
        }

        private void SpawnCustomer()
        {
            var spawner = FindObjectOfType<AI.CustomerSpawner>();
            if (spawner != null)
            {
                spawner.ForceSpawn();
                Log("Spawned customer!", Color.green);
            }
            else
            {
                Log("No CustomerSpawner found!", Color.red);
            }
        }

        private void Teleport(string[] args)
        {
            if (args.Length == 0)
            {
                Log("Usage: teleport [location]. Use 'locations' to see list.", Color.yellow);
                return;
            }

            string location = args[0].ToLower();
            Vector3? targetPos = null;

            switch (location)
            {
                case "bob":
                    var bob = FindObjectOfType<Core.BobCharacter>();
                    if (bob != null) targetPos = bob.transform.position + Vector3.back * 2;
                    break;

                case "shop":
                    var shop = FindObjectOfType<Economy.ShopManager>();
                    if (shop != null) targetPos = shop.transform.position;
                    break;

                case "register":
                    var register = FindObjectOfType<Economy.CashRegister>();
                    if (register != null) targetPos = register.transform.position + Vector3.back * 2;
                    break;

                case "spawn":
                case "home":
                    var spawn = FindObjectOfType<Core.CloneSpawnSystem>();
                    if (spawn != null && spawn.spawnTubes.Length > 0)
                        targetPos = spawn.spawnTubes[0].spawnPoint.position;
                    break;

                case "pump":
                    var pump = FindObjectOfType<Economy.GasPump>();
                    if (pump != null) targetPos = pump.transform.position + Vector3.back * 2;
                    break;
            }

            if (targetPos.HasValue)
            {
                var player = FindObjectOfType<Player.PlayerController>();
                if (player != null)
                {
                    var cc = player.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;
                    player.transform.position = targetPos.Value;
                    if (cc != null) cc.enabled = true;
                    Log($"Teleported to {location}!", Color.green);
                }
            }
            else
            {
                Log($"Location '{location}' not found!", Color.red);
            }
        }

        private void SetTime(string[] args)
        {
            if (args.Length == 0)
            {
                Log("Usage: time [hour] (0-24)", Color.yellow);
                return;
            }

            if (float.TryParse(args[0], out float hour))
            {
                var dayNight = FindObjectOfType<Systems.DayNightCycle>();
                if (dayNight != null)
                {
                    dayNight.SetTime(hour);
                    Log($"Time set to {hour:F1}:00", Color.green);
                }
                else
                {
                    Log("No DayNightCycle found!", Color.red);
                }
            }
        }

        private void SetGameSpeed(string[] args)
        {
            if (args.Length == 0)
            {
                Log($"Current speed: {Time.timeScale}x. Usage: speed [0.1-10]", Color.yellow);
                return;
            }

            if (float.TryParse(args[0], out float speed))
            {
                speed = Mathf.Clamp(speed, 0.1f, 10f);
                Time.timeScale = speed;
                Log($"Game speed: {speed}x", Color.green);
            }
        }

        private void BobCommands(string[] args)
        {
            if (args.Length == 0)
            {
                Log("Usage: bob [heal|damage amount]", Color.yellow);
                return;
            }

            var bob = FindObjectOfType<Core.BobCharacter>();
            if (bob == null)
            {
                Log("Bob not found!", Color.red);
                return;
            }

            switch (args[0])
            {
                case "heal":
                    bob.Feed(100); // Assuming Feed heals
                    Log($"Bob healed! Health: {bob.CurrentHealth}/{bob.maxHealth}", Color.green);
                    break;

                case "damage":
                    float damage = 10;
                    if (args.Length > 1) float.TryParse(args[1], out damage);
                    bob.TakeDamage(damage);
                    Log($"Bob damaged! Health: {bob.CurrentHealth}/{bob.maxHealth}", Color.red);
                    break;

                default:
                    Log($"Unknown bob command: {args[0]}", Color.red);
                    break;
            }
        }

        private void WinGame()
        {
            var gameState = Core.GameStateManager.Instance;
            if (gameState != null)
            {
                gameState.TriggerVictory();
                Log("Victory triggered!", Color.green);
            }
            else
            {
                Log("No GameStateManager found!", Color.red);
            }
        }

        private void LoseGame()
        {
            var gameState = Core.GameStateManager.Instance;
            if (gameState != null)
            {
                gameState.TriggerGameOver();
                Log("Game over triggered!", Color.red);
            }
        }

        private void ToggleFPS()
        {
            var hud = FindObjectOfType<UI.HUDManager>();
            if (hud != null)
            {
                hud.ToggleFPS();
                Log("FPS display toggled!", Color.green);
            }
        }

        private void ListItems()
        {
            Log("=== SPAWNABLE ITEMS ===", Color.yellow);
            Log("customer - Spawn a customer");
            // Add more as items system grows
        }

        private void ListLocations()
        {
            Log("=== TELEPORT LOCATIONS ===", Color.yellow);
            Log("bob - Bob's location");
            Log("shop - Shop entrance");
            Log("register - Cash register");
            Log("spawn/home - Spawn tubes");
            Log("pump - Gas pump");
        }

        private void ClearOutput()
        {
            outputLines.Clear();
            UpdateOutput();
        }

        #endregion

        #region Output

        public void Log(string message, Color? color = null)
        {
            string colorHex = color.HasValue ? ColorUtility.ToHtmlStringRGB(color.Value) : "FFFFFF";
            outputLines.Add($"<color=#{colorHex}>{message}</color>");

            while (outputLines.Count > maxOutputLines)
            {
                outputLines.RemoveAt(0);
            }

            UpdateOutput();
        }

        private void UpdateOutput()
        {
            if (outputText != null)
            {
                outputText.text = string.Join("\n", outputLines);
            }

            // Scroll to bottom
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        #endregion
    }
}
