using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using BobsPetroleum.Core;
using BobsPetroleum.UI;

namespace BobsPetroleum.Systems
{
    /// <summary>
    /// Quest and objective tracking system for guiding players through the game.
    /// Supports main quests, side quests, daily tasks, and achievements.
    /// </summary>
    public class QuestSystem : MonoBehaviour
    {
        public static QuestSystem Instance { get; private set; }

        [Header("Quest Configuration")]
        [Tooltip("All available quests")]
        public List<Quest> allQuests = new List<Quest>();

        [Header("Active Quests")]
        [Tooltip("Currently active quests")]
        public List<Quest> activeQuests = new List<Quest>();

        [Tooltip("Maximum active side quests")]
        public int maxActiveSideQuests = 3;

        [Header("Daily Tasks")]
        [Tooltip("Daily task templates")]
        public List<DailyTaskTemplate> dailyTaskTemplates = new List<DailyTaskTemplate>();

        [Tooltip("Current daily tasks")]
        public List<DailyTask> currentDailyTasks = new List<DailyTask>();

        [Tooltip("Tasks per day")]
        public int dailyTaskCount = 3;

        [Header("Tutorial")]
        [Tooltip("Tutorial quest (starts automatically)")]
        public Quest tutorialQuest;

        [Tooltip("Skip tutorial key")]
        public KeyCode skipTutorialKey = KeyCode.Tab;

        [Header("Audio")]
        public AudioClip questStartSound;
        public AudioClip questCompleteSound;
        public AudioClip objectiveCompleteSound;
        public AudioClip questFailedSound;
        public AudioClip dailyTaskCompleteSound;

        [Header("Events")]
        public UnityEvent<Quest> onQuestStarted;
        public UnityEvent<Quest> onQuestCompleted;
        public UnityEvent<Quest> onQuestFailed;
        public UnityEvent<QuestObjective> onObjectiveUpdated;
        public UnityEvent<QuestObjective> onObjectiveCompleted;
        public UnityEvent<DailyTask> onDailyTaskCompleted;

        // State
        private List<Quest> completedQuests = new List<Quest>();
        private AudioSource audioSource;
        private bool tutorialComplete = false;
        private Player.PlayerController cachedPlayer;
        private Player.PlayerInventory cachedInventory;

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
        }

        private void Start()
        {
            // Load completed quests from save
            LoadQuestProgress();

            // Start tutorial if not completed
            if (tutorialQuest != null && !tutorialComplete)
            {
                StartQuest(tutorialQuest);
            }

            // Generate daily tasks
            GenerateDailyTasks();

            // Subscribe to game events for quest tracking
            SubscribeToGameEvents();
        }

        private void Update()
        {
            // Skip tutorial
            if (Input.GetKeyDown(skipTutorialKey) && tutorialQuest != null &&
                activeQuests.Contains(tutorialQuest))
            {
                SkipTutorial();
            }
        }

        #region Quest Management

        /// <summary>
        /// Start a quest.
        /// </summary>
        public bool StartQuest(Quest quest)
        {
            if (quest == null) return false;
            if (activeQuests.Contains(quest)) return false;
            if (completedQuests.Contains(quest)) return false;

            // Check prerequisites
            foreach (var prereq in quest.prerequisites)
            {
                if (!completedQuests.Exists(q => q.questId == prereq))
                {
                    return false;
                }
            }

            // Check max side quests
            if (quest.questType == QuestType.SideQuest)
            {
                int activeSideQuests = activeQuests.FindAll(q => q.questType == QuestType.SideQuest).Count;
                if (activeSideQuests >= maxActiveSideQuests)
                {
                    HUDManager.Instance?.ShowNotification("Too many active side quests!");
                    return false;
                }
            }

            // Activate quest
            quest.isActive = true;
            quest.currentObjectiveIndex = 0;
            activeQuests.Add(quest);

            // Initialize objectives
            foreach (var obj in quest.objectives)
            {
                obj.currentProgress = 0;
                obj.isComplete = false;
            }

            // Play sound
            PlaySound(questStartSound);

            // Show notification
            HUDManager.Instance?.ShowNotification($"Quest Started: {quest.questName}");

            // Update HUD
            UpdateHUDObjective();

            // Set minimap waypoint for first objective
            UpdateQuestWaypoint(quest);

            onQuestStarted?.Invoke(quest);

            return true;
        }

        /// <summary>
        /// Complete a quest.
        /// </summary>
        public void CompleteQuest(Quest quest)
        {
            if (quest == null || !activeQuests.Contains(quest)) return;

            quest.isActive = false;
            quest.isComplete = true;
            activeQuests.Remove(quest);
            completedQuests.Add(quest);

            // Give rewards
            GiveQuestRewards(quest);

            // Play sound
            PlaySound(questCompleteSound);

            // Show notification
            HUDManager.Instance?.ShowNotification($"Quest Complete: {quest.questName}");

            // Update HUD
            UpdateHUDObjective();

            // Clear waypoint
            MinimapSystem.Instance?.ClearQuestWaypoint();

            // Save progress
            SaveQuestProgress();

            onQuestCompleted?.Invoke(quest);

            // Auto-start next quest if configured
            if (!string.IsNullOrEmpty(quest.nextQuestId))
            {
                Quest nextQuest = allQuests.Find(q => q.questId == quest.nextQuestId);
                if (nextQuest != null)
                {
                    StartQuest(nextQuest);
                }
            }
        }

        /// <summary>
        /// Fail a quest.
        /// </summary>
        public void FailQuest(Quest quest)
        {
            if (quest == null || !activeQuests.Contains(quest)) return;

            quest.isActive = false;
            activeQuests.Remove(quest);

            // Play sound
            PlaySound(questFailedSound);

            // Show notification
            HUDManager.Instance?.ShowNotification($"Quest Failed: {quest.questName}");

            // Update HUD
            UpdateHUDObjective();

            onQuestFailed?.Invoke(quest);
        }

        /// <summary>
        /// Abandon a quest.
        /// </summary>
        public void AbandonQuest(Quest quest)
        {
            if (quest == null || !activeQuests.Contains(quest)) return;
            if (quest.questType == QuestType.MainQuest) return; // Can't abandon main quests

            quest.isActive = false;
            activeQuests.Remove(quest);

            // Reset progress
            foreach (var obj in quest.objectives)
            {
                obj.currentProgress = 0;
                obj.isComplete = false;
            }

            HUDManager.Instance?.ShowNotification($"Quest Abandoned: {quest.questName}");
            UpdateHUDObjective();
        }

        #endregion

        #region Objective Tracking

        /// <summary>
        /// Update objective progress.
        /// </summary>
        public void UpdateObjective(string objectiveId, int progress)
        {
            foreach (var quest in activeQuests)
            {
                foreach (var objective in quest.objectives)
                {
                    if (objective.objectiveId == objectiveId && !objective.isComplete)
                    {
                        objective.currentProgress += progress;

                        // Check completion
                        if (objective.currentProgress >= objective.targetProgress)
                        {
                            objective.currentProgress = objective.targetProgress;
                            objective.isComplete = true;

                            PlaySound(objectiveCompleteSound);
                            HUDManager.Instance?.ShowNotification($"Objective Complete: {objective.description}");

                            onObjectiveCompleted?.Invoke(objective);

                            // Move to next objective or complete quest
                            quest.currentObjectiveIndex++;
                            if (quest.currentObjectiveIndex >= quest.objectives.Count)
                            {
                                CompleteQuest(quest);
                            }
                            else
                            {
                                UpdateQuestWaypoint(quest);
                                UpdateHUDObjective();
                            }
                        }
                        else
                        {
                            onObjectiveUpdated?.Invoke(objective);
                            UpdateHUDObjective();
                        }

                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Update objective by type (for automatic tracking).
        /// </summary>
        public void UpdateObjectiveByType(ObjectiveType type, int amount = 1)
        {
            foreach (var quest in activeQuests)
            {
                var currentObj = quest.GetCurrentObjective();
                if (currentObj != null && currentObj.objectiveType == type && !currentObj.isComplete)
                {
                    UpdateObjective(currentObj.objectiveId, amount);
                }
            }
        }

        #endregion

        #region Daily Tasks

        /// <summary>
        /// Generate new daily tasks.
        /// </summary>
        public void GenerateDailyTasks()
        {
            currentDailyTasks.Clear();

            if (dailyTaskTemplates.Count == 0) return;

            // Shuffle and pick tasks
            List<DailyTaskTemplate> shuffled = new List<DailyTaskTemplate>(dailyTaskTemplates);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }

            for (int i = 0; i < dailyTaskCount && i < shuffled.Count; i++)
            {
                var template = shuffled[i];
                currentDailyTasks.Add(new DailyTask
                {
                    taskId = template.taskId,
                    description = template.description,
                    objectiveType = template.objectiveType,
                    targetAmount = Random.Range(template.minTarget, template.maxTarget + 1),
                    currentProgress = 0,
                    isComplete = false,
                    rewardMoney = template.rewardMoney,
                    rewardXP = template.rewardXP
                });
            }
        }

        /// <summary>
        /// Update daily task progress.
        /// </summary>
        public void UpdateDailyTask(ObjectiveType type, int amount = 1)
        {
            foreach (var task in currentDailyTasks)
            {
                if (task.objectiveType == type && !task.isComplete)
                {
                    task.currentProgress += amount;

                    if (task.currentProgress >= task.targetAmount)
                    {
                        task.currentProgress = task.targetAmount;
                        task.isComplete = true;

                        // Give rewards using cached reference
                        var inventory = GetPlayerInventory();
                        inventory?.AddMoney(task.rewardMoney);

                        PlaySound(dailyTaskCompleteSound);
                        HUDManager.Instance?.ShowNotification($"Daily Task Complete: {task.description}");

                        onDailyTaskCompleted?.Invoke(task);
                    }
                }
            }
        }

        #endregion

        #region Tutorial

        /// <summary>
        /// Skip the tutorial.
        /// </summary>
        public void SkipTutorial()
        {
            if (tutorialQuest != null && activeQuests.Contains(tutorialQuest))
            {
                activeQuests.Remove(tutorialQuest);
                tutorialComplete = true;
                PlayerPrefs.SetInt("TutorialComplete", 1);

                HUDManager.Instance?.ShowNotification("Tutorial Skipped");
                UpdateHUDObjective();
            }
        }

        /// <summary>
        /// Reset tutorial.
        /// </summary>
        public void ResetTutorial()
        {
            tutorialComplete = false;
            PlayerPrefs.SetInt("TutorialComplete", 0);

            if (tutorialQuest != null)
            {
                tutorialQuest.isComplete = false;
                tutorialQuest.isActive = false;
                foreach (var obj in tutorialQuest.objectives)
                {
                    obj.currentProgress = 0;
                    obj.isComplete = false;
                }
            }
        }

        #endregion

        #region Rewards

        private Player.PlayerInventory GetPlayerInventory()
        {
            // Cache player reference for performance
            if (cachedPlayer == null)
            {
                cachedPlayer = FindObjectOfType<Player.PlayerController>();
            }
            if (cachedPlayer != null && cachedInventory == null)
            {
                cachedInventory = cachedPlayer.GetComponent<Player.PlayerInventory>();
            }
            return cachedInventory;
        }

        private void GiveQuestRewards(Quest quest)
        {
            var inventory = GetPlayerInventory();

            if (inventory != null)
            {
                inventory.AddMoney(quest.rewardMoney);
            }

            // Add items
            foreach (var item in quest.rewardItems)
            {
                inventory?.AddItem(item.itemId, item.quantity);
            }
        }

        #endregion

        #region HUD Integration

        private void UpdateHUDObjective()
        {
            if (HUDManager.Instance == null) return;

            // Get highest priority active quest
            Quest priorityQuest = null;
            foreach (var quest in activeQuests)
            {
                if (quest.questType == QuestType.MainQuest ||
                    (priorityQuest == null && quest.questType != QuestType.Tutorial))
                {
                    priorityQuest = quest;
                }
            }

            if (priorityQuest != null)
            {
                var objective = priorityQuest.GetCurrentObjective();
                if (objective != null)
                {
                    string text = objective.description;
                    if (objective.targetProgress > 1)
                    {
                        text += $" ({objective.currentProgress}/{objective.targetProgress})";
                    }
                    HUDManager.Instance.SetObjective(text);
                }
            }
            else
            {
                HUDManager.Instance.ClearObjective();
            }
        }

        private void UpdateQuestWaypoint(Quest quest)
        {
            var objective = quest.GetCurrentObjective();
            if (objective != null && objective.waypointPosition != Vector3.zero)
            {
                MinimapSystem.Instance?.SetQuestWaypoint(objective.waypointPosition);
            }
        }

        #endregion

        #region Game Event Subscriptions

        private void SubscribeToGameEvents()
        {
            // Subscribe to relevant game events for automatic tracking
            // These would be hooked up to actual game systems

            // Example: GameManager events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.onDayChange.AddListener(OnDayChanged);
                GameManager.Instance.onHamburgerFed.AddListener(OnHamburgerFed);
            }
        }

        private void UnsubscribeFromGameEvents()
        {
            // Unsubscribe to prevent memory leaks
            if (GameManager.Instance != null)
            {
                GameManager.Instance.onDayChange.RemoveListener(OnDayChanged);
                GameManager.Instance.onHamburgerFed.RemoveListener(OnHamburgerFed);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameEvents();
        }

        private void OnDayChanged()
        {
            // Generate new daily tasks each day
            GenerateDailyTasks();

            // Update day-based objectives
            UpdateObjectiveByType(ObjectiveType.SurviveDay);
        }

        private void OnHamburgerFed(int total)
        {
            UpdateObjectiveByType(ObjectiveType.FeedBob);
            UpdateDailyTask(ObjectiveType.FeedBob);
        }

        // Called by other systems:
        public void OnMoneyEarned(int amount)
        {
            UpdateObjectiveByType(ObjectiveType.EarnMoney, amount);
            UpdateDailyTask(ObjectiveType.EarnMoney, amount);
        }

        public void OnCustomerServed()
        {
            UpdateObjectiveByType(ObjectiveType.ServeCustomer);
            UpdateDailyTask(ObjectiveType.ServeCustomer);
        }

        public void OnZombieKilled()
        {
            UpdateObjectiveByType(ObjectiveType.KillZombie);
            UpdateDailyTask(ObjectiveType.KillZombie);
        }

        public void OnAnimalCaptured()
        {
            UpdateObjectiveByType(ObjectiveType.CaptureAnimal);
            UpdateDailyTask(ObjectiveType.CaptureAnimal);
        }

        public void OnItemCollected(string itemId)
        {
            UpdateObjectiveByType(ObjectiveType.CollectItem);
            UpdateDailyTask(ObjectiveType.CollectItem);
        }

        #endregion

        #region Save/Load

        private void SaveQuestProgress()
        {
            // Save completed quest IDs
            List<string> completedIds = new List<string>();
            foreach (var quest in completedQuests)
            {
                completedIds.Add(quest.questId);
            }

            string json = JsonUtility.ToJson(new QuestSaveData { completedQuestIds = completedIds });
            PlayerPrefs.SetString("QuestProgress", json);
            PlayerPrefs.Save();
        }

        private void LoadQuestProgress()
        {
            string json = PlayerPrefs.GetString("QuestProgress", "");
            if (!string.IsNullOrEmpty(json))
            {
                var saveData = JsonUtility.FromJson<QuestSaveData>(json);
                foreach (var questId in saveData.completedQuestIds)
                {
                    var quest = allQuests.Find(q => q.questId == questId);
                    if (quest != null)
                    {
                        quest.isComplete = true;
                        completedQuests.Add(quest);
                    }
                }
            }

            tutorialComplete = PlayerPrefs.GetInt("TutorialComplete", 0) == 1;
        }

        #endregion

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }

    [System.Serializable]
    public class Quest
    {
        public string questId;
        public string questName;
        [TextArea]
        public string description;
        public QuestType questType = QuestType.SideQuest;

        public List<QuestObjective> objectives = new List<QuestObjective>();
        public List<string> prerequisites = new List<string>();

        public int rewardMoney;
        public int rewardXP;
        public List<QuestRewardItem> rewardItems = new List<QuestRewardItem>();

        public string nextQuestId;

        [HideInInspector]
        public bool isActive;
        [HideInInspector]
        public bool isComplete;
        [HideInInspector]
        public int currentObjectiveIndex;

        public QuestObjective GetCurrentObjective()
        {
            if (currentObjectiveIndex < objectives.Count)
            {
                return objectives[currentObjectiveIndex];
            }
            return null;
        }
    }

    [System.Serializable]
    public class QuestObjective
    {
        public string objectiveId;
        public string description;
        public ObjectiveType objectiveType;
        public int targetProgress = 1;

        public Vector3 waypointPosition;

        [HideInInspector]
        public int currentProgress;
        [HideInInspector]
        public bool isComplete;
    }

    [System.Serializable]
    public class QuestRewardItem
    {
        public string itemId;
        public int quantity;
    }

    public enum QuestType
    {
        Tutorial,
        MainQuest,
        SideQuest,
        DailyQuest
    }

    public enum ObjectiveType
    {
        Talk,
        GoTo,
        CollectItem,
        KillZombie,
        CaptureAnimal,
        ServeCustomer,
        EarnMoney,
        FeedBob,
        SurviveDay,
        WinBattle,
        Custom
    }

    [System.Serializable]
    public class DailyTaskTemplate
    {
        public string taskId;
        public string description;
        public ObjectiveType objectiveType;
        public int minTarget;
        public int maxTarget;
        public int rewardMoney;
        public int rewardXP;
    }

    [System.Serializable]
    public class DailyTask
    {
        public string taskId;
        public string description;
        public ObjectiveType objectiveType;
        public int targetAmount;
        public int currentProgress;
        public bool isComplete;
        public int rewardMoney;
        public int rewardXP;
    }

    [System.Serializable]
    public class QuestSaveData
    {
        public List<string> completedQuestIds = new List<string>();
    }
}
