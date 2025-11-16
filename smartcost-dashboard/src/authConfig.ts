import { Configuration, PopupRequest } from "@azure/msal-browser";

/**
 * Configuration object to be passed to MSAL instance on creation. 
 * For a full list of MSAL.js configuration parameters, visit:
 * https://github.com/AzureAD/microsoft-authentication-library-for-js/blob/dev/lib/msal-browser/docs/configuration.md 
 */
export const msalConfig: Configuration = {
    auth: {
        clientId: process.env.REACT_APP_AZURE_AD_CLIENT_ID || "",
        authority: process.env.REACT_APP_AZURE_AD_AUTHORITY || "",
        redirectUri: process.env.REACT_APP_AZURE_AD_REDIRECT_URI || window.location.origin,
        postLogoutRedirectUri: process.env.REACT_APP_AZURE_AD_REDIRECT_URI || window.location.origin,
        navigateToLoginRequestUrl: true,
    },
    cache: {
        cacheLocation: "localStorage", // This configures where your cache will be stored
        storeAuthStateInCookie: false, // Set this to "true" if you are having issues on IE11 or Edge
    },
    system: {
        loggerOptions: {
            loggerCallback: (level, message, containsPii) => {
                if (containsPii) {
                    return;
                }
                switch (level) {
                    case 0: // Error
                        console.error(message);
                        return;
                    case 1: // Warning
                        console.warn(message);
                        return;
                    case 2: // Info
                        console.info(message);
                        return;
                    case 3: // Verbose
                        console.debug(message);
                        return;
                }
            }
        }
    }
};

/**
 * Scopes you add here will be prompted for user consent during sign-in.
 * By default, MSAL.js will add OIDC scopes (openid, profile, email) to any login request.
 * For more information about OIDC scopes, visit: 
 * https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-permissions-and-consent#openid-connect-scopes
 */
export const loginRequest: PopupRequest = {
    scopes: ["User.Read"]
};

/**
 * Add here the scopes to request when obtaining an access token for MS Graph API. For more information, see:
 * https://github.com/AzureAD/microsoft-authentication-library-for-js/blob/dev/lib/msal-browser/docs/resources-and-scopes.md
 */
export const graphConfig = {
    graphMeEndpoint: "https://graph.microsoft.com/v1.0/me"
};

/**
 * API scopes for SmartCost API
 */
export const apiConfig = {
    resourceUri: process.env.REACT_APP_API_BASE_URL || "",
    resourceScopes: [`api://${process.env.REACT_APP_AZURE_AD_CLIENT_ID}/user_impersonation`]
};
