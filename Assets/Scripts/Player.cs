using UnityEngine;
using Fusion;

public class Player : NetworkBehaviour
{
    public Material _material;

    [SerializeField] private Ball _prefabBall;
    [SerializeField] private PhysxBall _prefabPhysxBall;

    [Networked] private TickTimer delay { get; set; }

    [Networked] private bool spawnedProjectile1 { get; set; }
    [Networked] private bool spawnedProjectile2 { get; set; }
    private ChangeDetector _changeDetector;

    private NetworkCharacterController _cc;
    private Vector3 _forward;

    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterController>();
        _forward = transform.forward;

        _material = GetComponentInChildren<MeshRenderer>().material;
    }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void Render()
    {
        foreach(var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(spawnedProjectile1):
                    _material.color = Color.blue;
                    break;
                case nameof(spawnedProjectile2):
                    _material.color = Color.red;
                    break;
            }
        }

        _material.color = Color.Lerp(_material.color, Color.white, Time.deltaTime);
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            data.direction.Normalize();
            _cc.Move(5 * data.direction * Runner.DeltaTime);

            if (data.direction.sqrMagnitude > 0)
                _forward = data.direction;

            if (HasStateAuthority && delay.ExpiredOrNotRunning(Runner))
            {
                if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0))
                {
                    delay = TickTimer.CreateFromSeconds(Runner, 0.5f);
                    Runner.Spawn(_prefabBall,
                                transform.position + _forward,
                                Quaternion.LookRotation(_forward),
                                Object.InputAuthority,
                                (runner, o) =>
                                {
                                    o.GetComponent<Ball>().Init();
                                });
                    spawnedProjectile1 = !spawnedProjectile1;
                }
                else if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON1))
                {
                    delay = TickTimer.CreateFromSeconds(Runner, 0.5f);
                    Runner.Spawn(_prefabPhysxBall,
                                transform.position + _forward,
                                Quaternion.LookRotation(_forward),
                                Object.InputAuthority,
                                (runner, o) =>
                                {
                                    o.GetComponent<PhysxBall>().Init(10 * _forward);
                                });
                    spawnedProjectile2 = !spawnedProjectile2;
                }
            }
        }
    }
}