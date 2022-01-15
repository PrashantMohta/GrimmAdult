
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Modding;
using HutongGames.PlayMaker.Actions;

using static Modding.Logger;
using static Satchel.EnemyUtils;
using static Satchel.GameObjectUtils;
using static Satchel.FsmUtil;

namespace GrimmAdult
{   
    public enum State {
        Idle = 0,
        Turn,
        Walk,
        Teleport,
        Shoot
    }
    public enum Direction {
        Left = 0,
        Right
    }
    public class CompanionControl : MonoBehaviour
    {
        
        public tk2dSpriteAnimator animator;
        public Rigidbody2D rb;
        public BoxCollider2D collider;
        public AudioSource audioSource;

        public float scale = 0.5f;
        public float moveSpeed = 15f;
        public float IdleShuffleDistance = 0.1f;

        public GameObject followTarget;
        public float followDistance = 2f;
        public float teleportDistance = 15f;

        public AudioClip teleport,walk,yay;
        public Dictionary<State,string> Animations = new Dictionary<State,string>();

        public float followClipChance = 0.03f, teleportClipChance =  0.60f,turnClipChance = 0.30f, yayClipChance = 0.01f;
        private State state = State.Idle;
        private Direction lookDirection = Direction.Left;

        private bool changeDirection = false;
        private bool moveToNext = true;
        
        private void spawnBat(){
            Modding.Logger.Log("Attempt to spawn");
            var bat = GrimmAdult.Instance.GetBatFromPool();
            bat.transform.position = transform.position;
            Modding.Logger.Log(bat.transform.position);
            var fbc = bat.GetComponent<FireBatControl>();
            if(lookDirection == Direction.Left){
                bat.transform.position -= new Vector3(3f,0f,0f);
                fbc.lookDirection = Direction.Right;
            } else {
                bat.transform.position += new Vector3(3f,0f,0f);
                fbc.lookDirection = Direction.Left;
            }
            bat.SetActive(true);
            fbc.Init();
        }

        public GameObject getFollowTarget(){
            if(followTarget == null){
                followTarget = HeroController.instance.gameObject;
            }
            return followTarget;
        }

        void Start(){
            collider = gameObject.GetComponent<BoxCollider2D>();
            rb = gameObject.GetAddComponent<Rigidbody2D>();
            animator = gameObject.GetComponent<tk2dSpriteAnimator>();
            audioSource = gameObject.GetAddComponent<AudioSource>();

            rb.bodyType = RigidbodyType2D.Dynamic;
            collider.isTrigger = false;
            gameObject.SetScale(scale,scale);
            StartCoroutine(MainLoop());
        }

        public void playAnim(string clip,bool onlyNew = false) {
            if(animator != null){
                if(!onlyNew || !animator.IsPlaying(clip)){
                    animator.PlayFromFrame(clip, 0);
                }
            }
        }

        public void playAnimForState(bool onlyNew = false){
            if(Animations.TryGetValue(state, out var anim)){
                playAnim(anim,onlyNew);
            } else if(Animations.TryGetValue(State.Idle, out var fallbackAnim)){
                playAnim(fallbackAnim,onlyNew);
            }
        }

        private bool heroFartherThan(float distance){
            var displacement = getFollowTarget().transform.position - transform.position;
            return displacement.magnitude > distance;
        }

        private bool UpdateLookDirection(){
           Direction newLookDirection;
           changeDirection = false;

           if(getFollowTarget().transform.position.x > transform.position.x){
               newLookDirection = Direction.Right;
           } else {
               newLookDirection = Direction.Left;
           }
           if(newLookDirection != lookDirection){
               changeDirection = true;
               lookDirection = newLookDirection;
           }
           return changeDirection;
        }

        private void fixRotation(){
            //fix rotation
            gameObject.transform.rotation = Quaternion.identity;
        }
        private void playSound(AudioClip sound){
            if(Random.Range(0.0f, 1.0f) < 0.30f){
                audioSource.PlayOneShot(sound);
            }
        }
        private IEnumerator TurnToHero(){
           fixRotation();
           playAnimForState();
           if(teleport != null && !audioSource.isPlaying && Random.Range(0.0f, 1.0f) < turnClipChance){
               playSound(teleport);
           }
           yield return new WaitForSeconds(0.5f); // atleast stay here for this duration
           var ls = gameObject.transform.localScale;
           ls.x = (lookDirection == Direction.Left? -1f : 1f)*Mathf.Abs(ls.x);
           gameObject.transform.localScale = ls;
           state = State.Idle;
           moveToNext = true;
        }

        private void decideNextState(){
            var shouldFollowTarget = true;
            if(getFollowTarget() == HeroController.instance){
                shouldFollowTarget = HeroController.instance.cState.onGround; // || Random.Range(0.0f, 1.0f) < 0.3f
            }

            if(UpdateLookDirection()){
                state = State.Turn;
            } else if(heroFartherThan(teleportDistance) && shouldFollowTarget){
                state = State.Teleport;
            } else if(heroFartherThan(followDistance) && shouldFollowTarget){
                state = State.Walk;
            } else {
                state = State.Idle;
            }

            
            if(state == State.Idle && Random.Range(0.0f, 1.0f) < 0.3f){ // add check for enemies here later
                state = State.Shoot;
            } 
        }
        private IEnumerator Idle(){
            fixRotation();
            playAnimForState(true);
            if(yay != null && !audioSource.isPlaying && Random.Range(0.0f, 1.0f) < yayClipChance){
                playSound(yay);
            }
            rb.velocity = new Vector2(0.0f, 0.0f);

            var force = new Vector2(Random.Range(-1.0f, 1.0f),Random.Range(-1.0f, 1.0f));
            yield return rb.moveTowards(-force,IdleShuffleDistance,0.2f);
            
            decideNextState();;
            yield return new WaitForSeconds(0.1f); // atleast stay idle for this duration
            moveToNext = true;
        }
        private IEnumerator Teleport(){
            //play Teleport animation
            fixRotation();
            if(teleport != null && !audioSource.isPlaying && Random.Range(0.0f, 1.0f) < teleportClipChance){
                playSound(teleport);
            }
            playAnimForState();
            var deltaToPlayer = Random.Range(-0.5f, 0.5f);
            if(Random.Range(0.0f, 1.0f) < 0.50f){
                gameObject.transform.position = getFollowTarget().transform.position + new Vector3(0.5f + deltaToPlayer,2f,0f);
            } else {
                gameObject.transform.position = getFollowTarget().transform.position + new Vector3(-0.5f + deltaToPlayer,2f,0f);
            }
            yield return new WaitForSeconds(0.1f); // atleast stay here for this duration

            state = State.Idle;
            moveToNext = true;
        }

        private IEnumerator Follow(){
            //play follow animation
            playAnimForState(true);
            if(walk != null && !audioSource.isPlaying && Random.Range(0.0f, 1.0f) < followClipChance){
                playSound(walk);
            }

            //get displacement to player
            Vector2 displacement;
            displacement = getFollowTarget().transform.position - transform.position;
            displacement += new Vector2(Random.Range(-0.01f, 0.01f),Random.Range(-0.01f, 0.01f));

            var followDistanceR = followDistance * (1f+Random.Range(-0.25f, 0.50f));
            var distance = Mathf.Min(teleportDistance,displacement.magnitude-followDistanceR);
            var moveSpeedR = moveSpeed * (1f+Random.Range(-0.25f, 0.50f));
            yield return rb.moveTowards(displacement,distance*0.5f,distance*0.5f/moveSpeedR);
            playAnimForState(true);
            yield return rb.moveTowards(displacement,distance*0.5f,distance*0.5f/moveSpeedR);


            decideNextState();
            moveToNext = true;
        }
        private IEnumerator ShootFireBat(){
            fixRotation();
            var ls = gameObject.transform.localScale;
            ls.x = (lookDirection == Direction.Left? 1f : -1f)*Mathf.Abs(ls.x);
            gameObject.transform.localScale = ls;

            playAnimForState(true);
            if(yay != null && !audioSource.isPlaying && Random.Range(0.0f, 1.0f) < yayClipChance){
                playSound(yay);
            }
            spawnBat();
            yield return new WaitForSeconds(0.2f); // stay for this duration
            spawnBat();
            yield return new WaitForSeconds(0.1f); // stay for this duration
            spawnBat();
            yield return new WaitForSeconds(0.1f); // stay for this duration
            decideNextState();
            moveToNext = true;
        }
        private IEnumerator MainLoop()
        {
            while(true){
                yield return new WaitWhile(()=>!moveToNext);
                moveToNext = false;
                //Log(state);
                if(state == State.Idle){
                    StartCoroutine(Idle());
                }else if(state == State.Walk){
                    //follow player move towards player (but keep some distance)
                    StartCoroutine(Follow());
                }else if(state == State.Teleport){
                    //follow player move towards player (but keep some distance)
                    StartCoroutine(Teleport());
                } else if(state == State.Turn){
                    StartCoroutine(TurnToHero());
                } else if(state == State.Shoot){
                    StartCoroutine(ShootFireBat());
                }
            }
        }
    }
}