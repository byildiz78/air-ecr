using System.Collections.Generic;

namespace Ecr.Module.Services.Ingenico.Recovery
{
    /// <summary>
    /// Recovery action detayı
    /// </summary>
    public class RecoveryAction
    {
        public RecoveryActionType ActionType { get; set; }
        public string Description { get; set; }
        public int Priority { get; set; } // 1 = highest
        public bool IsRequired { get; set; }
        public Dictionary<string, object> Parameters { get; set; }

        public RecoveryAction()
        {
            Description = string.Empty;
            Priority = 5;
            IsRequired = false;
            Parameters = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Recovery plan - sıralı action listesi
    /// </summary>
    public class RecoveryPlan
    {
        public RecoveryStrategy Strategy { get; set; }
        public List<RecoveryAction> Actions { get; set; }
        public string Description { get; set; }
        public bool RequiresUserAction { get; set; }
        public string UserActionMessage { get; set; }

        public RecoveryPlan()
        {
            Actions = new List<RecoveryAction>();
            Description = string.Empty;
            RequiresUserAction = false;
            UserActionMessage = string.Empty;
        }

        /// <summary>
        /// Action ekle
        /// </summary>
        public void AddAction(RecoveryActionType actionType, string description, int priority = 5, bool isRequired = true)
        {
            Actions.Add(new RecoveryAction
            {
                ActionType = actionType,
                Description = description,
                Priority = priority,
                IsRequired = isRequired
            });
        }
    }
}