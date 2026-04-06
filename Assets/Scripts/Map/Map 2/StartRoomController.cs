using PLAYERTWO.PlatformerProject;
using System.Collections;
using UnityEngine;

public class StartRoomController : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject spawnPoint;
    [SerializeField] private GameObject effect01Prefab;
    [SerializeField] private GameObject effect02Prefab;
    [SerializeField] private RoomManager roomManager;
    private GameObject effect01Obj;
    private GameObject effect02Obj;
    private ParticleSystem effect01;
    private ParticleSystem effect02;
    private DungeonController dungeonController;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        StartCoroutine(SetCamera());
    }

    private IEnumerator SetCamera()
    {
        yield return new WaitForSeconds(1f);
        roomManager = transform.parent.GetComponentInParent<RoomManager>();
        dungeonController = GameObject.FindGameObjectWithTag("DungeonController").GetComponent<DungeonController>();

        if (dungeonController != null)
        {
            dungeonController.SwitchCinemachine(roomManager.BaseCamera);
            roomManager.roomblank.SetActive(false);
        }
        StartCoroutine(SpawnEffectCouroutine());
    }

    private IEnumerator SpawnEffectCouroutine()
    {
        yield return new WaitForSeconds(2f);
        

        effect01Obj = Instantiate(effect01Prefab, spawnPoint.transform.position, Quaternion.Euler(90f, 0f, 0f));
        effect02Obj = Instantiate(effect02Prefab, spawnPoint.transform.position, Quaternion.Euler(90f, 0f, 0f));

        effect01 = effect01Obj.GetComponent<ParticleSystem>();
        effect01.Play();

        effect02 = effect02Obj.GetComponent<ParticleSystem>();
        effect02.Play();
        yield return new WaitForSeconds(0.5f);
        player.transform.position = spawnPoint.transform.position;
        yield return new WaitForSeconds(1f);
        player.GetComponent<EntityController>().enabled = true;
    }
}