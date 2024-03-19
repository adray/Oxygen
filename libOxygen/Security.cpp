#include "Security.h"
#include "openssl\crypto.h"

#include <openssl/conf.h>
#include <openssl/evp.h>
#include <openssl/err.h>

using namespace Oxygen;

Security::Security()
{
    ERR_load_crypto_strings();
    OpenSSL_add_all_algorithms();
    OPENSSL_config(NULL);
}

void handleErrors()
{

}

void Security::SHA256(const std::string& str, unsigned char** digest, unsigned int* digest_len)
{
    EVP_MD_CTX* mdctx;

    if ((mdctx = EVP_MD_CTX_new()) == NULL)
	    handleErrors();

    if (1 != EVP_DigestInit_ex(mdctx, EVP_sha256(), NULL))
	    handleErrors();

    if (1 != EVP_DigestUpdate(mdctx, str.c_str(), str.size()))
	    handleErrors();

    if ((*digest = (unsigned char*)OPENSSL_malloc(EVP_MD_size(EVP_sha256()))) == NULL)
	    handleErrors();

    if (1 != EVP_DigestFinal_ex(mdctx, *digest, digest_len))
	    handleErrors();

    EVP_MD_CTX_free(mdctx);
}

Security::~Security()
{
    EVP_cleanup();
    CRYPTO_cleanup_all_ex_data();
    ERR_free_strings();
}
