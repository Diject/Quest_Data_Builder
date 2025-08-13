using Quest_Data_Builder.TES3.Handlers;

namespace Quest_Data_Builder.TES3.Variables
{
    enum LeveledListItemType
    {
        Unknown,
        Item,
        LeveledList,
    }

    internal class LeveledListItem
    {
        public string Id { get; set; }
        public uint Level { get; set; }

        public LeveledListItemType Type { get; set; } = LeveledListItemType.Unknown;

        /// <summary>
        /// <typeparamref name="LeveledItem"/> parent of this item if it is a leveled list
        /// </summary>
        public object? Object;

        public LeveledListItem(string Id, uint Level)
        {
            this.Id = Id;
            this.Level = Level;
        }

        public void IdentifyType(LeveledItemHandler? handler)
        {
            if (handler is null || this.Type != LeveledListItemType.Unknown) return;

            if (handler.TryGetValue(this.Id, out var levItem))
            {
                this.Type = LeveledListItemType.LeveledList;
                this.Object = levItem;
            }
            else
            {
                this.Type = LeveledListItemType.Item;
            }
        }
    }
}
