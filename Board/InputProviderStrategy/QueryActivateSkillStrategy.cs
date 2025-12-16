using System;
using System.Threading.Tasks;
using ArC.CardGames.Predefined.Vanguard;

public class QueryActivateSkillStrategy : IInputProviderStrategy, IQueryActivateSkill
{
    public Task<bool> QueryActivateSkill(VanguardSkillCost SkillCost)
    {
        throw new NotImplementedException();
    }
}