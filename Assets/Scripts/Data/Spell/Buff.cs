public class Buff
{
    public BuffType type;
    public int bonus_pa;
    public int bonus_pm;
    public float damage_resistance;
    public int turn_duration;


    public void ApplyOn(Character targetCharacter)
    {
        targetCharacter.pa.max += bonus_pa; targetCharacter.pa.current += bonus_pa;
        targetCharacter.pm.max += bonus_pm; targetCharacter.pm.current += bonus_pm;
        targetCharacter.damage_resistance += damage_resistance;
    }


    public void RemoveOn(Character targetCharacter)
    {
        targetCharacter.pa.max -= bonus_pa; targetCharacter.pa.current -= bonus_pa;
        targetCharacter.pm.max -= bonus_pm; targetCharacter.pm.current -= bonus_pm;
        targetCharacter.damage_resistance -= damage_resistance;
    }


    public void UpdateTurn()
    {
        turn_duration--;
    }

    
    public bool IsDurationOver()
    {
        return turn_duration <= 0;
    }
}


public enum BuffType {
    BUFF,
    DEBUFF
}