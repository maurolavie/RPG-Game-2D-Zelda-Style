using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Player : MonoBehaviour {

    public float speed = 4f;

    Animator anim;
    Rigidbody2D rb2d;
    Vector2 mov; // Ahora es visible entre los métodos

    CircleCollider2D attackCollider;

    public GameObject initialMap;
    public GameObject slashPrefab;

    bool movePrevent;

    Aura aura;

    void Awake() {
        Assert.IsNotNull(initialMap);
        Assert.IsNotNull(slashPrefab);
    }

    // Start is called before the first frame update
    void Start() {
        anim = GetComponent<Animator>();
        rb2d = GetComponent<Rigidbody2D>();

        // Recuperamos el collider de ataque del primer hijo
        attackCollider = transform.GetChild(0).GetComponent<CircleCollider2D>();
        // Lo desactivamos desde el principio, luego
        attackCollider.enabled = false;

        Camera.main.GetComponent<MainCamera>().SetBound(initialMap);

        aura = transform.GetChild(1).GetComponent<Aura>();
    }

    // Update is called once per frame
    void Update() {
        
        Movements();

        Animations();

        SwordAttack();

        SlashAttack();

        PreventMovement();

        /*
        Vector3 mov = new Vector3(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"),
            0
        );

        transform.position = Vector3.MoveTowards(
            transform.position,
            transform.position + mov,
            speed * Time.deltaTime
        );
        */

    }

    void FixedUpdate() {
        // Nos movemos en el fixed por las físicas
        rb2d.MovePosition(rb2d.position + mov * speed * Time.deltaTime);
    }

    void Movements(){
        
        // Detectamos el movimiento en un vector 2D
        mov = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

    }

    void Animations() {
        if (mov != Vector2.zero) {
            anim.SetFloat("movX", mov.x);
            anim.SetFloat("movY", mov.y);
            anim.SetBool("walking", true);
        } else {
            anim.SetBool("walking", false);
        }
    }

    void SwordAttack() {
        
        // Buscamos el estado actual mirando la información del animador
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        bool attacking = stateInfo.IsName("Player_Attack");

        // Detectamos el ataque, tiene prioridad por lo que va abajo del todo
        if (Input.GetKeyDown("space") && !attacking ){
            anim.SetTrigger("attacking");
        }

        // Vamos actualizando la posición de la colisión de ataque
        if (mov != Vector2.zero) attackCollider.offset = new Vector2(mov.x/2, mov.y/2);

        // Activamos el collider a la mitad de la animación de ataque
        if(attacking) {
            float playbackTime = stateInfo.normalizedTime;
            if (playbackTime > 0.33 && playbackTime < 0.66) attackCollider.enabled = true;
            else attackCollider.enabled = false;
        }
    }

    void SlashAttack() {
        // Buscamos el estado actual mirando la información del animador
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        bool loading = stateInfo.IsName("Player_Slash");

        // Ataque a distancia
        if (Input.GetKeyDown(KeyCode.LeftAlt)){
            anim.SetTrigger("loading");
            aura.AuraStart();
        } else if (Input.GetKeyUp(KeyCode.LeftAlt)){
            anim.SetTrigger("attacking");
            if(aura.IsLoaded()){
                // Conseguir la rotación a partir de un vector
                float angle = Mathf.Atan2(
                    anim.GetFloat("movY"),
                    anim.GetFloat("movX")
                ) * Mathf.Rad2Deg;
                // Creamos la instancia del slash
                GameObject slashObj = Instantiate(
                    slashPrefab, transform.position,
                    Quaternion.AngleAxis(angle, Vector3.forward)
                );
                // Le otorgamos el movimiento inicial
                Slash slash = slashObj.GetComponent<Slash>();
                slash.mov.x = anim.GetFloat("movX");
                slash.mov.y = anim.GetFloat("movY");
            }
            aura.AuraStop();
            //Esperar unos momentos y reactivar el movimiento
            StartCoroutine(EnabledMovementAfter(0.4f));
        }

        // Prevenimos el movimiento mientras cargamos
        if (loading) {
            movePrevent = true;
        }
    }

    void PreventMovement() {
        if (movePrevent) {
            mov = Vector2.zero;
        }
    }

    IEnumerator EnabledMovementAfter(float seconds){
        yield return new WaitForSeconds(seconds);
        movePrevent = false;
    }

}