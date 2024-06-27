using System.Runtime.CompilerServices;
using UnityEngine;

namespace Components.CardEffect
{
    public abstract class ACardEffect
    {
        protected CardData Data;

        protected ACardEffect(CardData data)
        {
            Data = data;
        }

        protected GameObject GetGameManager([CallerMemberName] string memberName = "")
        {
            var manager = GameObject.FindGameObjectWithTag("GameManager");
            if (manager == null)
            {
                Debug.LogError(memberName + " : Can't find tag with GameManager");
            }

            return manager;
        }

        public GameObject GetCaster(bool isAlly, [CallerMemberName] string memberName = "")
        {
            var caster =
                GetGameManager()
                    !.GetComponent<MainGameManager>()
                    !.GetCharacterManager()
                    !.FindCharacterWithName(isAlly, Data.characterName);

            if (caster == null)
            {
                Debug.LogError(memberName + " : Can't find caster(" + Data.characterName + ")");
            }

            return caster;
        }

        public Animator GetCasterAnimator(bool isAlly, [CallerMemberName] string memberName = "")
        {
            var caster = GetCaster(isAlly)!;
            var animator = caster.GetComponent<Animator>();

            if (caster == null)
            {
                Debug.LogError(memberName + " : Can't get caster(" + Data.characterName + ")'s animator");
            }

            return animator;
        }

        // Return false if not usable
        public virtual bool Usable()
        {
            return false;
        }

        public virtual bool CastInFrontOfTarget()
        {
            return true;
        }

        public abstract void Apply(bool isAllyCasting, GameObject target);
    }
}