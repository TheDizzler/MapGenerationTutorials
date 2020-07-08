using UnityEngine;

namespace AtomosZ.Tutorials.CellAuto
{
	public class Player2D : MonoBehaviour
	{
		Rigidbody2D body;
		private Vector2 velocity;


		void Start()
		{
			body = GetComponent<Rigidbody2D>();
		}

		void Update()
		{
			velocity = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized * 10;
		}

		void FixedUpdate()
		{
			body.MovePosition(body.position + velocity * Time.deltaTime);
		}
	}
}