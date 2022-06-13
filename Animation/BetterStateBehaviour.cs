using UnityEngine;

// this is from u/DarianLP
// https://www.reddit.com/r/Unity3D/comments/varuk4/a_better_statemachinebehaviour/

/*

The problem:

StateMachineBehaviour is great, but the Animator calls OnStateEnter for the new state, then OnStateUpdate for both states while a transition is happening, then finally OnStateExit for the old state. I get why it is like that (e.g., Enter new State -> Transition -> Exit old State), but this leads to some behavior that is hard to work around/account for.

BetterStateBehaviour will keep track of when we are entering and exiting the Animator State and will call the new the following methods in a more sequential fashion:

OnEnter
OnUpdate
OnExit
OnMove
OnIk

This way you can be sure that OnUpdate will be called as long as you are in the state, but as soon as you start to transition to a different state it will stop and allow the next state to take full control.

*/
    
    public class BetterStateBehaviour : StateMachineBehaviour
    {
        private bool _isEntering;
        private bool _isExiting;
        private bool _isInTransition;
    
        public virtual void OnEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
        public virtual void OnUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
        public virtual void OnExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
        public virtual void OnMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
        public virtual void OnIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
    
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _isEntering = true;
            _isExiting = false;
            OnEnter(animator, stateInfo, layerIndex);
        }
    
        override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _isInTransition = animator.IsInTransition(layerIndex);
            if (_isEntering)
            {
                if (!_isInTransition) _isEntering = false;
                OnUpdate(animator, stateInfo, layerIndex);
            }
            else // !_isEntering
            {
                if (_isInTransition)
                {
                    if (!_isExiting)
                    {
                        _isExiting = true;
                        OnExit(animator, stateInfo, layerIndex);
                    }
                }
                else // !_isInTransition
                {
                    OnUpdate(animator, stateInfo, layerIndex);
                }
            }
        }
    
        override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!_isExiting)
            {
                OnMove(animator, stateInfo, layerIndex);
            }
        }
    
        override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!_isExiting)
            {
                OnIK(animator, stateInfo, layerIndex);
            }
        }
    }