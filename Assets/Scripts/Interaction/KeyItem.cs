using UnityEngine;

namespace EscapeRoom.Interaction
{
    /// <summary>
    /// Specialized PickupItem for physical keys required by LockedContainers or Doors.
    /// Default key ID is 'room_key'.
    /// </summary>
    [DisallowMultipleComponent]
    public class KeyItem : PickupItem
    {
        [Header("Key Defaults")]
        [SerializeField] private string defaultKeyId = "room_key";
        [SerializeField] private string defaultKeyName = "Room Key";

        private void Reset()
        {
            ConfigureItem(defaultKeyId, defaultKeyName, "A small metallic key needed to unlock a container.");
        }

        private void Awake()
        {
            // Ensure default key id is populated if not explicitly configured
            if (string.IsNullOrEmpty(ItemId) || ItemId == "item_id")
            {
                ConfigureItem(defaultKeyId, defaultKeyName, "A small metallic key needed to unlock a container.");
            }
        }
    }
}
