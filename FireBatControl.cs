
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
    public class FireBatControl : MonoBehaviour
    {
        public Vector2 velocity = new Vector2(20f,0f);
        public Rigidbody2D rb;
        public Direction lookDirection = Direction.Left;

        void Start(){

        }

        public void Init(){
            rb = gameObject.GetAddComponent<Rigidbody2D>();
            var anim = gameObject.GetComponent<tk2dSpriteAnimator>();
            anim.Play("Firebat");
            gameObject.SetScale(0.4f,0.4f);
            if(lookDirection == Direction.Left){
                rb.velocity = velocity ;
                gameObject.transform.localScale = new Vector3(0.4f,gameObject.transform.localScale.y,gameObject.transform.localScale.z);
            } else {
                rb.velocity = -velocity;
                gameObject.transform.localScale = new Vector3(-0.4f,gameObject.transform.localScale.y,gameObject.transform.localScale.z);
            }
            rb.velocity += new Vector2(Random.Range(-0.01f, 0.01f),Random.Range(-0.01f, 0.01f));
            StartCoroutine(DisableCoro());
        }

        private IEnumerator DisableCoro(){
            yield return new WaitForSeconds(1f);
            gameObject.SetActive(false);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            gameObject.SetActive(false);
        }

    }
}