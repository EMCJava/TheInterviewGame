using System;
using System.Collections.Generic;

namespace Components
{
    [Serializable]
    public struct CardData
    {
        [NonSerialized] public CardEffect.ACardEffect Actor;

        public string name;
        public string color;
        public string displayName;
        public string actorName;
        public string characterName;

        public CardData(string name, string color, string displayName, string actorName, string characterName)
        {
            this.name = name;
            this.color = color;
            this.displayName = displayName;
            this.actorName = actorName;
            this.characterName = characterName;
            this.Actor = null;

            LoadActor();
        }

        public void LoadActor()
        {
            var t = Type.GetType(actorName) ?? Type.GetType("Components.CardEffect." + actorName);
            Actor = t != null ? (CardEffect.ACardEffect)Activator.CreateInstance(t, this) : null;
        }
    }

    [Serializable]
    public struct CardDataList
    {
        public List<CardData> config;
    }
}