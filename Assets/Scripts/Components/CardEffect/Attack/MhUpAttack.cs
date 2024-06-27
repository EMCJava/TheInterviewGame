using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Components.CardEffect.Attack
{
    public class MhUpAttack : ACardEffect
    {
        private static readonly int AnimatorUpAttack = Animator.StringToHash("UpAttack");
        private static readonly int AnimatorTakeHit = Animator.StringToHash("TakeHit");

        private readonly AudioClip _attackSound;

        public MhUpAttack(CardData data) : base(data)
        {
            _attackSound = Resources.Load<AudioClip>("Audio/CardEffect/Attack/sword_attack");
        }

        public override bool Usable()
        {
            return true;
        }

        private async UniTaskVoid ApplyDamage(GameObject target)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.25f), ignoreTimeScale: false); // wait for animation
            AudioSource.PlayClipAtPoint(_attackSound, target.transform.position);
            target.GetComponent<Character>()?.ChangeHealth(-10);
            target.GetComponent<Animator>()!.SetTrigger(AnimatorTakeHit);
        }

        public override void Apply(bool isAllyCasting, GameObject target)
        {
            GetCasterAnimator(isAllyCasting)!.SetTrigger(AnimatorUpAttack);
            ApplyDamage(target).Forget();
        }
    }
}