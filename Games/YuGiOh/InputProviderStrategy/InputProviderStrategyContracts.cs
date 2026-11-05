using System.Threading.Tasks;
using ArC.CardGames.YuGiOh;

public interface ISelectChainResponse
{
    Task<YuGiOhChainResponse?> SelectChainResponse(YuGiOhChain chain);
}