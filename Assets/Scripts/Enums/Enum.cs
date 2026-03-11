namespace PLAYERTWO.PlatformerProject
{
    public enum CannonFireMode
    {
        Projectile,
        Beam
    }

    public enum ParticleDamageMode
    {
        SprayTrigger,
        BulletTrigger
    }

    public enum ExtraAttackMode
    {
        None,
        Animated
    }

    public enum EnemyAttackType
    {
        NormalHit = 0,
        RollAttack = 1,
        RangedShot = 2,
        SprayAttack = 3
    }

    public enum AttackAnimMode
    {
        Trigger = 0,
        Bool = 1
    }

    public enum StoryPageState
    {
        Intro,          // Đang chờ intro page / overlay
        Typing,         // Đang chạy chữ
        Ready,          // Đã xong, chờ bấm next
        Transitioning   // Đang chuyển sang page khác
    }
}