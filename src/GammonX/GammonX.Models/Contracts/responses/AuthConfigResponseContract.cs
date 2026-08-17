using System.Runtime.Serialization;

namespace GammonX.Models.Contracts
{
    /// <summary>
    /// Provides the public Cognito user pool coordinates the mobile client needs
    /// to construct its <c>CognitoUserPool</c> before any user is signed in.
    /// </summary>
    /// <remarks>
    /// The member names are lower camel case on purpose -- the client destructures
    /// them verbatim in <c>src/api/cognito.ts</c>. Neither value is a secret; the
    /// route is served unauthenticated (see <c>public_routes</c> in components/agw).
    /// </remarks>
    [DataContract]
    public sealed class AuthConfigResponseContract : BaseResponseContract
    {
        [DataMember(Name = "userPoolId")]
        public string UserPoolId { get; set; } = string.Empty;

        [DataMember(Name = "clientId")]
        public string ClientId { get; set; } = string.Empty;
    }
}
