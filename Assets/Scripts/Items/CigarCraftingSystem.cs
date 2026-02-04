using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace BobsPetroleum.Items
{
    /// <summary>
    /// Cigar crafting system - roll cigars at the lab table that give special powers!
    /// Buy the lab table from a vendor, then craft cigars with different effects.
    /// </summary>
    public class CigarCraftingSystem : MonoBehaviour
    {
        public static CigarCraftingSystem Instance { get; private set; }

        [Header("Lab Table")]
        [Tooltip("Does player own the lab table?")]
        public bool ownsLabTable = false;

        [Tooltip("Lab table price")]
        public int labTablePrice = 500;

        [Tooltip("Lab table object (enabled when owned)")]
        public GameObject labTableObject;

        [Tooltip("Lab table interaction range")]
        public float interactionRange = 3f;

        [Header("Cigar Recipes")]
        [Tooltip("All available cigar recipes")]
        public List<CigarRecipe> recipes = new List<CigarRecipe>();

        [Header("Crafting")]
        [Tooltip("Time to roll one cigar")]
        public float craftTime = 3f;

        [Tooltip("Currently crafting?")]
        public bool isCrafting = false;

        [Header("UI")]
        [Tooltip("Crafting panel")]
        public GameObject craftingPanel;

        [Tooltip("Recipe list container")]
        public Transform recipeListContainer;

        [Tooltip("Recipe button prefab")]
        public GameObject recipeButtonPrefab;

        [Tooltip("Selected recipe display")]
        public TMP_Text selectedRecipeName;

        [Tooltip("Selected recipe description")]
        public TMP_Text selectedRecipeDesc;

        [Tooltip("Selected recipe ingredients")]
        public TMP_Text selectedRecipeIngredients;

        [Tooltip("Craft button")]
        public Button craftButton;

        [Tooltip("Progress bar")]
        public Image craftProgressBar;

        [Tooltip("Result display")]
        public TMP_Text resultText;

        [Header("Audio")]
        public AudioClip tableInteractSound;
        public AudioClip craftingLoopSound;
        public AudioClip craftCompleteSound;
        public AudioClip craftFailSound;

        [Header("Visual")]
        [Tooltip("Smoke particle during crafting")]
        public ParticleSystem craftingSmoke;

        [Header("Events")]
        public UnityEvent onLabTablePurchased;
        public UnityEvent<CigarRecipe> onCraftStarted;
        public UnityEvent<CigarRecipe> onCraftComplete;

        // State
        private CigarRecipe selectedRecipe;
        private float craftProgress = 0f;
        private AudioSource audioSource;
        private AudioSource loopAudioSource;
        private bool panelOpen = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Loop audio for crafting
            GameObject loopObj = new GameObject("CraftingLoop");
            loopObj.transform.SetParent(transform);
            loopAudioSource = loopObj.AddComponent<AudioSource>();
            loopAudioSource.loop = true;
            loopAudioSource.clip = craftingLoopSound;
        }

        private void Start()
        {
            // Load ownership
            ownsLabTable = PlayerPrefs.GetInt("OwnsLabTable", 0) == 1;

            // Show/hide table
            if (labTableObject != null)
            {
                labTableObject.SetActive(ownsLabTable);
            }

            // Hide panel
            if (craftingPanel != null)
            {
                craftingPanel.SetActive(false);
            }

            // Setup default recipes if empty
            if (recipes.Count == 0)
            {
                SetupDefaultRecipes();
            }
        }

        private void Update()
        {
            // Crafting progress
            if (isCrafting)
            {
                craftProgress += Time.deltaTime / craftTime;

                if (craftProgressBar != null)
                {
                    craftProgressBar.fillAmount = craftProgress;
                }

                if (craftProgress >= 1f)
                {
                    CompleteCraft();
                }
            }

            // Check for nearby player interaction
            if (ownsLabTable && !panelOpen)
            {
                CheckPlayerInteraction();
            }
        }

        #region Lab Table Purchase

        /// <summary>
        /// Purchase the lab table.
        /// </summary>
        public bool PurchaseLabTable()
        {
            var inventory = FindObjectOfType<Player.PlayerInventory>();
            if (inventory == null) return false;

            if (inventory.Money < labTablePrice)
            {
                UI.HUDManager.Instance?.ShowNotification("Not enough money for lab table!");
                return false;
            }

            inventory.RemoveMoney(labTablePrice);
            ownsLabTable = true;

            PlayerPrefs.SetInt("OwnsLabTable", 1);
            PlayerPrefs.Save();

            if (labTableObject != null)
            {
                labTableObject.SetActive(true);
            }

            UI.HUDManager.Instance?.ShowNotification("Lab table purchased! You can now craft cigars.");
            onLabTablePurchased?.Invoke();

            return true;
        }

        #endregion

        #region Interaction

        private void CheckPlayerInteraction()
        {
            if (labTableObject == null) return;

            var player = FindObjectOfType<Player.PlayerController>();
            if (player == null) return;

            float distance = Vector3.Distance(labTableObject.transform.position, player.transform.position);

            if (distance <= interactionRange)
            {
                UI.HUDManager.Instance?.ShowInteractionPrompt("Press E to use Lab Table");

                if (Input.GetKeyDown(KeyCode.E))
                {
                    OpenCraftingPanel();
                }
            }
        }

        #endregion

        #region Crafting Panel

        public void OpenCraftingPanel()
        {
            if (!ownsLabTable)
            {
                UI.HUDManager.Instance?.ShowNotification("You need to buy a lab table first!");
                return;
            }

            panelOpen = true;

            if (craftingPanel != null)
            {
                craftingPanel.SetActive(true);
            }

            // Disable player
            var player = FindObjectOfType<Player.PlayerController>();
            if (player != null)
            {
                player.enabled = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            PlaySound(tableInteractSound);
            PopulateRecipeList();
        }

        public void CloseCraftingPanel()
        {
            panelOpen = false;

            if (craftingPanel != null)
            {
                craftingPanel.SetActive(false);
            }

            // Enable player
            var player = FindObjectOfType<Player.PlayerController>();
            if (player != null)
            {
                player.enabled = true;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Stop crafting if in progress
            if (isCrafting)
            {
                CancelCraft();
            }
        }

        private void PopulateRecipeList()
        {
            if (recipeListContainer == null) return;

            // Clear
            foreach (Transform child in recipeListContainer)
            {
                Destroy(child.gameObject);
            }

            // Add recipes
            foreach (var recipe in recipes)
            {
                GameObject btn = null;

                if (recipeButtonPrefab != null)
                {
                    btn = Instantiate(recipeButtonPrefab, recipeListContainer);
                }
                else
                {
                    btn = new GameObject(recipe.cigarName);
                    btn.transform.SetParent(recipeListContainer);
                    btn.AddComponent<RectTransform>();
                    var button = btn.AddComponent<Button>();
                }

                var text = btn.GetComponentInChildren<TMP_Text>();
                if (text != null)
                {
                    text.text = recipe.cigarName;
                }

                var button2 = btn.GetComponent<Button>();
                if (button2 != null)
                {
                    CigarRecipe capturedRecipe = recipe;
                    button2.onClick.AddListener(() => SelectRecipe(capturedRecipe));
                }
            }
        }

        public void SelectRecipe(CigarRecipe recipe)
        {
            selectedRecipe = recipe;

            if (selectedRecipeName != null)
            {
                selectedRecipeName.text = recipe.cigarName;
            }

            if (selectedRecipeDesc != null)
            {
                selectedRecipeDesc.text = recipe.description + "\n\nEffects:\n";
                foreach (var effect in recipe.effects)
                {
                    selectedRecipeDesc.text += $"• {effect.effectType}: {effect.value} ({effect.duration}s)\n";
                }
            }

            if (selectedRecipeIngredients != null)
            {
                selectedRecipeIngredients.text = "Ingredients:\n";
                foreach (var ing in recipe.ingredients)
                {
                    int have = ConsumableSystem.Instance?.GetItemCount(ing.itemId) ?? 0;
                    string color = have >= ing.quantity ? "green" : "red";
                    selectedRecipeIngredients.text += $"<color={color}>• {ing.itemName} x{ing.quantity} (Have: {have})</color>\n";
                }
            }

            // Update craft button
            if (craftButton != null)
            {
                craftButton.interactable = CanCraft(recipe);
            }
        }

        #endregion

        #region Crafting

        public bool CanCraft(CigarRecipe recipe)
        {
            if (recipe == null) return false;
            if (isCrafting) return false;

            // Check ingredients
            foreach (var ing in recipe.ingredients)
            {
                if (!ConsumableSystem.Instance.HasItem(ing.itemId, ing.quantity))
                {
                    return false;
                }
            }

            return true;
        }

        public void StartCraft()
        {
            if (selectedRecipe == null) return;
            if (!CanCraft(selectedRecipe)) return;

            // Consume ingredients
            foreach (var ing in selectedRecipe.ingredients)
            {
                ConsumableSystem.Instance.RemoveItem(ing.itemId, ing.quantity);
            }

            isCrafting = true;
            craftProgress = 0f;

            if (craftProgressBar != null)
            {
                craftProgressBar.fillAmount = 0f;
                craftProgressBar.gameObject.SetActive(true);
            }

            if (craftingSmoke != null)
            {
                craftingSmoke.Play();
            }

            if (loopAudioSource != null && craftingLoopSound != null)
            {
                loopAudioSource.Play();
            }

            if (resultText != null)
            {
                resultText.text = $"Rolling {selectedRecipe.cigarName}...";
            }

            onCraftStarted?.Invoke(selectedRecipe);
        }

        private void CompleteCraft()
        {
            isCrafting = false;
            craftProgress = 0f;

            // Stop effects
            if (craftingSmoke != null)
            {
                craftingSmoke.Stop();
            }

            if (loopAudioSource != null)
            {
                loopAudioSource.Stop();
            }

            // Add cigar to inventory
            ConsumableSystem.Instance?.AddItem(selectedRecipe.resultItemId, 1);

            PlaySound(craftCompleteSound);

            if (resultText != null)
            {
                resultText.text = $"Crafted {selectedRecipe.cigarName}!";
            }

            UI.HUDManager.Instance?.ShowNotification($"Crafted {selectedRecipe.cigarName}!");

            onCraftComplete?.Invoke(selectedRecipe);

            // Refresh UI
            SelectRecipe(selectedRecipe);
        }

        public void CancelCraft()
        {
            if (!isCrafting) return;

            isCrafting = false;
            craftProgress = 0f;

            if (craftingSmoke != null)
            {
                craftingSmoke.Stop();
            }

            if (loopAudioSource != null)
            {
                loopAudioSource.Stop();
            }

            // Note: ingredients are already consumed, no refund
            if (resultText != null)
            {
                resultText.text = "Craft cancelled (ingredients lost)";
            }
        }

        #endregion

        #region Default Recipes

        private void SetupDefaultRecipes()
        {
            // Speed Cigar
            recipes.Add(new CigarRecipe
            {
                cigarName = "Speed Demon Cigar",
                description = "A potent blend that makes you faster than a caffeinated cheetah.",
                resultItemId = "cigar_speed",
                ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { itemId = "tobacco", itemName = "Tobacco", quantity = 2 },
                    new CraftingIngredient { itemId = "energy_herb", itemName = "Energy Herb", quantity = 1 }
                },
                effects = new List<ConsumableEffect>
                {
                    new ConsumableEffect { effectType = EffectType.SpeedBoost, value = 5f, duration = 60f }
                }
            });

            // Damage Cigar
            recipes.Add(new CigarRecipe
            {
                cigarName = "Rage Stogie",
                description = "Smoke this and your attacks hit like a truck.",
                resultItemId = "cigar_damage",
                ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { itemId = "tobacco", itemName = "Tobacco", quantity = 2 },
                    new CraftingIngredient { itemId = "red_pepper", itemName = "Red Pepper", quantity = 2 }
                },
                effects = new List<ConsumableEffect>
                {
                    new ConsumableEffect { effectType = EffectType.DamageBoost, value = 2f, duration = 45f }
                }
            });

            // Healing Cigar
            recipes.Add(new CigarRecipe
            {
                cigarName = "Medicinal Stogie",
                description = "The healthy choice. Ironic, isn't it?",
                resultItemId = "cigar_heal",
                ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { itemId = "tobacco", itemName = "Tobacco", quantity = 1 },
                    new CraftingIngredient { itemId = "healing_herb", itemName = "Healing Herb", quantity = 2 }
                },
                effects = new List<ConsumableEffect>
                {
                    new ConsumableEffect { effectType = EffectType.Heal, value = 50f, duration = 0f }
                }
            });

            // Invincibility Cigar
            recipes.Add(new CigarRecipe
            {
                cigarName = "Iron Lung Special",
                description = "Nothing can hurt you. Except maybe lung cancer.",
                resultItemId = "cigar_invincible",
                ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { itemId = "tobacco", itemName = "Tobacco", quantity = 3 },
                    new CraftingIngredient { itemId = "rare_mushroom", itemName = "Rare Mushroom", quantity = 1 },
                    new CraftingIngredient { itemId = "iron_dust", itemName = "Iron Dust", quantity = 1 }
                },
                effects = new List<ConsumableEffect>
                {
                    new ConsumableEffect { effectType = EffectType.Invincibility, value = 1f, duration = 15f }
                }
            });

            // Night Vision Cigar
            recipes.Add(new CigarRecipe
            {
                cigarName = "Cat's Eye Cigar",
                description = "See in the dark like a spooky cat.",
                resultItemId = "cigar_nightvision",
                ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { itemId = "tobacco", itemName = "Tobacco", quantity = 2 },
                    new CraftingIngredient { itemId = "glow_fungus", itemName = "Glow Fungus", quantity = 2 }
                },
                effects = new List<ConsumableEffect>
                {
                    new ConsumableEffect { effectType = EffectType.NightVision, value = 1f, duration = 120f }
                }
            });
        }

        #endregion

        #region Audio

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        #endregion
    }

    [System.Serializable]
    public class CigarRecipe
    {
        public string cigarName;
        [TextArea(2, 3)]
        public string description;
        public string resultItemId;
        public Sprite icon;
        public List<CraftingIngredient> ingredients = new List<CraftingIngredient>();
        public List<ConsumableEffect> effects = new List<ConsumableEffect>();
    }

    [System.Serializable]
    public class CraftingIngredient
    {
        public string itemId;
        public string itemName;
        public int quantity;
    }
}
