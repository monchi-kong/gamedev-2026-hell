using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxBackground : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField, Range(0f, 1f)] private float parallaxFactor = 0.5f;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool affectVertical = false;

    private float spriteWidth;
    private Vector3 previousCameraPosition;
    private Transform leftCopy;
    private Transform rightCopy;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        if (cameraTransform != null)
        {
            previousCameraPosition = cameraTransform.position;
        }

        if (loop)
        {
            spriteWidth = GetComponent<SpriteRenderer>().bounds.size.x;
            leftCopy = CreateCopy(-spriteWidth);
            rightCopy = CreateCopy(spriteWidth);
        }
    }

    private Transform CreateCopy(float xOffset)
    {
        GameObject copy = Instantiate(gameObject, transform.position + Vector3.right * xOffset, transform.rotation, transform.parent);
        Destroy(copy.GetComponent<ParallaxBackground>());
        return copy.transform;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 delta = cameraTransform.position - previousCameraPosition;
        float verticalDelta = affectVertical ? delta.y * parallaxFactor : 0f;
        Vector3 move = new Vector3(delta.x * parallaxFactor, verticalDelta, 0f);

        transform.position += move;
        if (leftCopy != null) leftCopy.position += move;
        if (rightCopy != null) rightCopy.position += move;

        previousCameraPosition = cameraTransform.position;

        if (loop && leftCopy != null && rightCopy != null)
        {
            float distance = cameraTransform.position.x - transform.position.x;
            if (distance > spriteWidth)
            {
                Shift(spriteWidth);
            }
            else if (distance < -spriteWidth)
            {
                Shift(-spriteWidth);
            }
        }
    }

    private void Shift(float dx)
    {
        transform.position += Vector3.right * dx;
        leftCopy.position += Vector3.right * dx;
        rightCopy.position += Vector3.right * dx;
    }
}
