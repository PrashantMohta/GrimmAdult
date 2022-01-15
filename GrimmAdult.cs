using System;
using System.Collections;
using System.Collections.Generic;

using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;


using HutongGames.PlayMaker.Actions;

using static Modding.Logger;
using static Satchel.GameObjectUtils;
using static Satchel.FsmUtil;
using static Satchel.EnemyUtils;
using static Satchel.SpriteUtils;
using static Satchel.TextureUtils;

namespace GrimmAdult
{
    public class GrimmAdult : Mod 
    {

        public static GrimmAdult Instance;
        public GameObject prefab,companion;

        public List<GameObject> bats = new List<GameObject>();
        
        public GameObject GetBatFromPool(){
           bats.RemoveAll(b => b == null);
           var bat = bats.Find(b => !b.activeInHierarchy);
           if(bat == null){
                Modding.Logger.Log("None in Pool");
                bat = createBatCompanion(HeroController.instance.gameObject);
                bats.Add(bat);
           }
           return bat;
        }

        public override string GetVersion()
        {
            return "0.1";
        }
        public void removeComponentFromAllChildren<T>(GameObject gameObject) where T : Component {
            if( gameObject == null ){ return; }
            while(gameObject.RemoveComponent<T>()){};
            foreach( var t in gameObject.GetComponentsInChildren<Transform>( true ) )
            {
                if(t.gameObject != gameObject){
                    removeComponentFromAllChildren<T>(t.gameObject);
                }
            }
            return;
        }

        public void removePFSMFromAllChildren(GameObject gameObject){
            removeComponentFromAllChildren<PlayMakerFSM>(gameObject);
        }

        public static bool RemoveSpecificFsm(GameObject go,string name){
            PlayMakerFSM comp = go.GetComponent<PlayMakerFSM>();
            Modding.Logger.Log(comp.FsmName);
            if(comp != null && comp.FsmName == name){
                GameObject.DestroyImmediate(comp);
                return true;
            }
            return false;
        }
        public GameObject createBatCompanion(GameObject ft = null){
            if(prefab == null) { return null; }
            var grimm = prefab.createCompanionFromPrefab();
            GameObject.DontDestroyOnLoad(grimm);
            grimm.layer = 17;
            removePFSMFromAllChildren(grimm);            
            removeComponentFromAllChildren<DamageHero>(grimm);
            //add control and adjust parameters
            var gc = grimm.GetAddComponent<FireBatControl>();
            var de = grimm.GetAddComponent<DamageEnemies>();

            // set params for damage values here, steal nail value here ig?
            de.attackType = AttackTypes.Nail;
            de.circleDirection = false;
            de.damageDealt = 5;
            de.direction = 180f;
            de.ignoreInvuln = false;
            de.magnitudeMult = 2f;
            de.moveDirection = false;
            de.specialType = SpecialTypes.None;
            grimm.SetActive(true);

            var anim = grimm.GetComponent<tk2dSpriteAnimator>();
            anim.Play("Firebat");

            //fix up collider size
            var collider = grimm.GetAddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.0f,0.5f);
            collider.offset = new Vector2(0f,-0.4f);
            collider.enabled = true;

            return grimm;
        }
        public GameObject createGrimmCompanion(GameObject ft = null){
            if(prefab == null) { return null; }
            var grimm = prefab.createCompanionFromPrefab();
            GameObject.DontDestroyOnLoad(grimm);
            grimm.layer = 17;
            removePFSMFromAllChildren(grimm);            
            removeComponentFromAllChildren<DamageHero>(grimm);
            //add control and adjust parameters
            var gc = grimm.GetAddComponent<CompanionControl>();
            var de = grimm.GetAddComponent<DamageEnemies>();
            // set params for damage values here, steal nail value here ig?
            de.attackType = AttackTypes.Nail;
            de.circleDirection = false;
            de.damageDealt = 5;
            de.direction = 180f;
            de.ignoreInvuln = false;
            de.magnitudeMult = 2f;
            de.moveDirection = false;
            de.specialType = SpecialTypes.None;

            if(ft != null){
                gc.followTarget = ft;
            }
            grimm.GetAddComponent<Rigidbody2D>().gravityScale = 1f;
            //add control and adjust parameters
            gc.moveSpeed = 9f;
            gc.followDistance = 2f;
            gc.IdleShuffleDistance = 0.01f;

            //fix up collider size
            var collider = grimm.GetAddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.5f,8.0f);
            collider.offset = new Vector2(0f,-0.4f);
            collider.enabled = true;
            
            // add animations
            gc.Animations.Add(State.Idle,"Idle");
            gc.Animations.Add(State.Walk,"Evade");
            gc.Animations.Add(State.Turn,"TeleToIdle");
            gc.Animations.Add(State.Teleport,"TeleToIdle");
            gc.Animations.Add(State.Shoot,"Cast");

            // extract audios
            

            grimm.SetActive(true);
            return grimm;
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Instance = this;
            prefab = preloadedObjects["GG_Grimm"]["Grimm Scene/Grimm Boss"];
            
            UnityEngine.Object.DontDestroyOnLoad(prefab);
            ModHooks.HeroUpdateHook += update;
        }
       
        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>
            {
                ("GG_Grimm", "Grimm Scene/Grimm Boss")
            };   
        }
        
        public void EnsureGrimmCompanion(){
            if(companion == null){
                companion = createGrimmCompanion(HeroController.instance.gameObject);
            }
        }
        public int i = 0;
        public void update()
        {
            EnsureGrimmCompanion();
            if(Input.GetKeyDown(KeyCode.L)){
                var bat = createBatCompanion(HeroController.instance.gameObject);
                //companion.LogWithChildren();
                bat.transform.position = HeroController.instance.gameObject.transform.position + new Vector3(1,1,0);
                var gc = bat.GetAddComponent<FireBatControl>();
                gc.Init();
            }
            if(Input.GetKeyDown(KeyCode.P)){
                //companion.LogWithChildren();
                companion.transform.position = HeroController.instance.gameObject.transform.position + new Vector3(1,1,0);
                var anim = companion.GetComponent<tk2dSpriteAnimator>();
                var clips = anim.Library.clips;
                foreach(var c in clips){
                    Modding.Logger.Log(c.name);
                }
            }
            if(Input.GetKeyDown(KeyCode.N)){
                var anim = companion.GetComponent<tk2dSpriteAnimator>();
                var clips = anim.Library.clips;
                if(i < clips.Length - 1){
                    i++;
                } else {
                    i = 0;
                }
                Modding.Logger.Log(clips[i].name);
                anim.Play(clips[i]);
            }
        }
        
    }

}
