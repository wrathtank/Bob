using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace BobsPetroleum.Battle
{
    /// <summary>
    /// UI for the Pokemon-style battle system.
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        [Header("Player Animal Info")]
        public TMP_Text playerAnimalName;
        public TMP_Text playerAnimalHealth;
        public Slider playerHealthBar;

        [Header("Enemy Animal Info")]
        public TMP_Text enemyAnimalName;
        public TMP_Text enemyAnimalHealth;
        public Slider enemyHealthBar;

        [Header("Attack Buttons")]
        public Transform attackButtonContainer;
        public GameObject attackButtonPrefab;

        [Header("Party Panel")]
        public Transform partyContainer;
        public GameObject partyMemberPrefab;

        [Header("Action Buttons")]
        public Button fightButton;
        public Button switchButton;
        public Button runButton;

        [Header("Panels")]
        public GameObject mainActionPanel;
        public GameObject attackPanel;
        public GameObject switchPanel;

        [Header("Message Display")]
        public TMP_Text messageText;

        private BattleManager battleManager;
        private List<GameObject> attackButtons = new List<GameObject>();
        private List<GameObject> partyButtons = new List<GameObject>();

        public void Initialize(BattleManager manager)
        {
            battleManager = manager;

            // Setup button listeners
            if (fightButton != null)
                fightButton.onClick.AddListener(ShowAttackPanel);

            if (switchButton != null)
                switchButton.onClick.AddListener(ShowSwitchPanel);

            if (runButton != null)
                runButton.onClick.AddListener(OnRunClicked);

            // Subscribe to events
            battleManager.onAttackUsed.AddListener(OnAttackUsed);
            battleManager.onAnimalFainted.AddListener(OnAnimalFainted);
            battleManager.onAnimalSwitched.AddListener(OnAnimalSwitched);

            // Initial UI setup
            RefreshUI();
            ShowMainPanel();
        }

        private void OnDestroy()
        {
            if (battleManager != null)
            {
                battleManager.onAttackUsed.RemoveListener(OnAttackUsed);
                battleManager.onAnimalFainted.RemoveListener(OnAnimalFainted);
                battleManager.onAnimalSwitched.RemoveListener(OnAnimalSwitched);
            }
        }

        private void Update()
        {
            if (battleManager == null) return;

            RefreshHealthBars();

            // Enable/disable based on turn
            bool canAct = battleManager.IsPlayerTurn && battleManager.IsBattleActive;
            if (mainActionPanel != null)
            {
                mainActionPanel.SetActive(canAct && !attackPanel.activeSelf && !switchPanel.activeSelf);
            }
        }

        private void RefreshUI()
        {
            RefreshHealthBars();
            RefreshAttackButtons();
            RefreshPartyButtons();
        }

        private void RefreshHealthBars()
        {
            var playerAnimal = battleManager?.GetCurrentPlayerAnimal();
            var enemyAnimal = battleManager?.GetCurrentEnemyAnimal();

            // Player
            if (playerAnimal != null)
            {
                if (playerAnimalName != null)
                    playerAnimalName.text = playerAnimal.animalName;

                if (playerAnimalHealth != null)
                    playerAnimalHealth.text = $"{Mathf.CeilToInt(playerAnimal.currentHealth)}/{playerAnimal.maxHealth}";

                if (playerHealthBar != null)
                    playerHealthBar.value = playerAnimal.currentHealth / playerAnimal.maxHealth;
            }

            // Enemy
            if (enemyAnimal != null)
            {
                if (enemyAnimalName != null)
                    enemyAnimalName.text = enemyAnimal.animalName;

                if (enemyAnimalHealth != null)
                    enemyAnimalHealth.text = $"{Mathf.CeilToInt(enemyAnimal.currentHealth)}/{enemyAnimal.maxHealth}";

                if (enemyHealthBar != null)
                    enemyHealthBar.value = enemyAnimal.currentHealth / enemyAnimal.maxHealth;
            }
        }

        private void RefreshAttackButtons()
        {
            // Clear existing
            foreach (var btn in attackButtons)
            {
                Destroy(btn);
            }
            attackButtons.Clear();

            if (attackButtonContainer == null || attackButtonPrefab == null)
                return;

            var playerAnimal = battleManager?.GetCurrentPlayerAnimal();
            if (playerAnimal == null) return;

            for (int i = 0; i < playerAnimal.attacks.Count; i++)
            {
                var attack = playerAnimal.attacks[i];
                var btnObj = Instantiate(attackButtonPrefab, attackButtonContainer);

                var text = btnObj.GetComponentInChildren<TMP_Text>();
                if (text != null)
                {
                    text.text = $"{attack.attackName}\n({attack.damage} dmg)";
                }

                var button = btnObj.GetComponent<Button>();
                if (button != null)
                {
                    int attackIndex = i;
                    button.onClick.AddListener(() => OnAttackSelected(attackIndex));
                }

                attackButtons.Add(btnObj);
            }
        }

        private void RefreshPartyButtons()
        {
            // Clear existing
            foreach (var btn in partyButtons)
            {
                Destroy(btn);
            }
            partyButtons.Clear();

            if (partyContainer == null || partyMemberPrefab == null)
                return;

            var playerAnimals = battleManager?.GetPlayerAnimals();
            if (playerAnimals == null) return;

            for (int i = 0; i < playerAnimals.Count; i++)
            {
                var animal = playerAnimals[i];
                var btnObj = Instantiate(partyMemberPrefab, partyContainer);

                var text = btnObj.GetComponentInChildren<TMP_Text>();
                if (text != null)
                {
                    string status = animal.currentHealth > 0 ? $"{Mathf.CeilToInt(animal.currentHealth)}/{animal.maxHealth}" : "Fainted";
                    text.text = $"{animal.animalName}\n{status}";
                }

                var button = btnObj.GetComponent<Button>();
                if (button != null)
                {
                    int animalIndex = i;
                    button.onClick.AddListener(() => OnSwitchSelected(animalIndex));
                    button.interactable = animal.currentHealth > 0;
                }

                partyButtons.Add(btnObj);
            }
        }

        #region Panel Navigation

        public void ShowMainPanel()
        {
            if (mainActionPanel != null) mainActionPanel.SetActive(true);
            if (attackPanel != null) attackPanel.SetActive(false);
            if (switchPanel != null) switchPanel.SetActive(false);
        }

        public void ShowAttackPanel()
        {
            if (mainActionPanel != null) mainActionPanel.SetActive(false);
            if (attackPanel != null) attackPanel.SetActive(true);
            if (switchPanel != null) switchPanel.SetActive(false);

            RefreshAttackButtons();
        }

        public void ShowSwitchPanel()
        {
            if (mainActionPanel != null) mainActionPanel.SetActive(false);
            if (attackPanel != null) attackPanel.SetActive(false);
            if (switchPanel != null) switchPanel.SetActive(true);

            RefreshPartyButtons();
        }

        public void BackToMain()
        {
            ShowMainPanel();
        }

        #endregion

        #region Button Handlers

        private void OnAttackSelected(int attackIndex)
        {
            battleManager?.PlayerSelectAttack(attackIndex);
            ShowMainPanel();
        }

        private void OnSwitchSelected(int animalIndex)
        {
            battleManager?.PlayerSwitchAnimal(animalIndex);
            ShowMainPanel();
        }

        private void OnRunClicked()
        {
            battleManager?.RunFromBattle();
        }

        #endregion

        #region Event Handlers

        private void OnAttackUsed(AnimalAttack attack, BattleAnimal target)
        {
            if (messageText != null)
            {
                messageText.text = $"{attack.attackName} dealt {attack.damage} damage!";
            }

            RefreshHealthBars();
        }

        private void OnAnimalFainted(BattleAnimal animal)
        {
            if (messageText != null)
            {
                messageText.text = $"{animal.animalName} fainted!";
            }

            RefreshUI();
        }

        private void OnAnimalSwitched(BattleAnimal newAnimal)
        {
            if (messageText != null)
            {
                messageText.text = $"Go, {newAnimal.animalName}!";
            }

            RefreshUI();
        }

        #endregion
    }
}
