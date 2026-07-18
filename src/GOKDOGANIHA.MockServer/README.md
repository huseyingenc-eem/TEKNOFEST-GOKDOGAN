# GOKDOGANIHA Mock Competition Server

TEKNOFEST Savaşan İHA haberleşme API'sini yerelde taklit eder.
Yedi resmî endpoint'i, oturum cookie'sini, 1–2 Hz telemetri sınırını ve paket
alan doğrulamasını uygular.

## Çalıştırma

```bash
dotnet run --project src/GOKDOGANIHA.MockServer
```

GCS ayarları:

- Sunucu adresi: `http://127.0.0.1:5000`
- `Test / mock sunucu profili`: açık
- Kullanıcı adı ve şifre: boş olmayan herhangi bir değer

## Senaryolar

```bash
curl -X POST http://127.0.0.1:5000/api/mock/scenario/hss-active
curl -X POST http://127.0.0.1:5000/api/mock/scenario/dense-opponents
curl -X POST http://127.0.0.1:5000/api/mock/scenario/no-hss
curl -X POST http://127.0.0.1:5000/api/mock/reset
```

Durum:

```bash
curl http://127.0.0.1:5000/api/mock/state
```

Resmî endpoint'ler girişten sonra cookie ister. Ham API testi için:

```bash
curl -c /tmp/gokdogan-cookie.txt \
  -H 'Content-Type: application/json' \
  -d '{"kadi":"gokdogan","sifre":"test"}' \
  http://127.0.0.1:5000/api/giris

curl -b /tmp/gokdogan-cookie.txt \
  http://127.0.0.1:5000/api/sunucusaati
```
