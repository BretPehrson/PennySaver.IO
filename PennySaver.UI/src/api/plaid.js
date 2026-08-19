import api from "./client";

export const createLinkToken = async () => {
    const response = await api.post("/plaid/create-link-token");
    return response.data.link_token;
};

export const exchangePublicToken = async ({ publicToken, plaidAccountId, institutionName, institutionId }) => {
    const response = await api.post("/plaid/exchange-public-token", {
        publicToken,
        plaidAccountId,
        institutionName,
        institutionId,
    });
    return response.data;
};
