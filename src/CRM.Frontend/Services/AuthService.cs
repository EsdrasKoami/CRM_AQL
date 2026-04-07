using System.IdentityModel.Tokens.Jwt;

namespace CRM.Frontend.Services;

/// <summary>
/// Stocke le JWT en mémoire et expose les infos de l'utilisateur connecté.
/// Le token n'est JAMAIS écrit sur le disque.
/// </summary>
public class AuthService
{
    public string?   Token    { get; private set; }
    public string?   Role     { get; private set; }
    public string?   Nom      { get; private set; }
    public string?   Prenom   { get; private set; }
    public bool      IsLoggedIn => Token is not null;

    public bool IsDirecteurFinances => Role == "DirecteurFinances";
    public bool IsAgent             => Role == "Agent" || IsDirecteurFinances;

    public void SetSession(LoginResponse resp)
    {
        Token  = resp.Token;
        Role   = resp.Role;
        Nom    = resp.Nom;
        Prenom = resp.Prenom;
    }

    public void Logout()
    {
        Token  = null;
        Role   = null;
        Nom    = null;
        Prenom = null;
    }

    /// <summary>Vérifie si le JWT est encore valide (non expiré).</summary>
    public bool IsTokenValid()
    {
        if (Token is null) return false;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt     = handler.ReadJwtToken(Token);
            return jwt.ValidTo > DateTime.UtcNow.AddMinutes(1);
        }
        catch { return false; }
    }
}
