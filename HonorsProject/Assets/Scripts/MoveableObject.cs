using UnityEngine; 

public class MoveableObject : MonoBehaviour
{

    //These fields will be exposed to Unity so the dev can set the parameters there
    [SerializeField] private float speed = 5f;




    // Use this for initialization
    void Start()
    {
 
    }
    
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKey(KeyCode.W))
        {
            transform.Translate(Vector3.up * speed * Time.deltaTime);
        }
        if(Input.GetKey(KeyCode.S))
        {
            transform.Translate(Vector3.down * speed * Time.deltaTime);
        }
        if(Input.GetKey(KeyCode.A))
        {
            transform.Translate(Vector3.left * speed * Time.deltaTime);
        }
        if(Input.GetKey(KeyCode.D))
        {
            transform.Translate(Vector3.right * speed * Time.deltaTime);
        }
    }


}
