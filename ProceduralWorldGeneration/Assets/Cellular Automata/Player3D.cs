using UnityEngine;

namespace AtomosZ.Tutorials.CellAuto
{
	public class Player3D : MonoBehaviour
	{
		Rigidbody body;
		private Vector3 velocity;


		void Start()
		{
			body = GetComponent<Rigidbody>();
		}

		void Update()
		{
			velocity = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized * 10;
		}

		void FixedUpdate()
		{
			body.MovePosition(body.position + velocity * Time.deltaTime);
		}
	}
}