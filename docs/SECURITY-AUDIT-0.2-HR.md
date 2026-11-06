# Brendigo USB Key — preliminarni sigurnosni i dead-code pregled

Pregled koda i CI-a: 6. studenoga 2026. Neovisni penetracijski test, formalni audit i stvarno testiranje na nekoliko fizičkih USB stickova još nisu provedeni.

## Učinjena poboljšanja

| Rizik / problem | Mjera |
| --- | --- |
| Nevaljano kodiranje USB tokena | Provjera JSON formata i canonical Base64, točno 256 bita |
| Prevelika datoteka na izmjenjivom disku | Ograničenje parsiranja na 2048 bajtova |
| Jednostavno preusmjeravanje preko junction/symlink datoteke | Odbijanje reparse-point datoteka i mapa |
| Kratko nestajanje USB-a | Tri uzastopne odsutne provjere prije zaključavanja |
| Pogrešan zapis o uspješnom zaključavanju | Razlikovanje prihvaćenog i odbijenog zahtjeva LockWorkStation |
| Neograničen sigurnosni dnevnik | Lokalna rotacija na približno 1 MiB |
| Neuspjeh upisa opcije auto-lock | Vraćanje prethodne vrijednosti u memoriji |
| Pogreške formata tokena | Automatizirani deterministički smoke testovi u CI-u |

## Rizici koji i dalje postoje

1. **Visok:** datoteka na običnom USB-u može se kopirati, stoga nije kriptografski neklonabilna.
2. **Visok:** aplikacija još nema Windows Credential Provider; nije sustav za otključavanje Windows prijave.
3. **Srednji:** isključivanje WPF procesa onemogućuje daljnji auto-lock; nije sustavski servis.
4. **Srednji:** polling može propustiti promjene i zahtijeva testiranje fizičkih USB uređaja.
5. **Srednji:** provjere symlink putanja smanjuju rizik, ali ne rješavaju sve TOCTOU napade na sadržaj USB-a.
6. **Srednji:** DPAPI CurrentUser ne štiti od zlonamjernog procesa koji već radi u istom prijavljenom korisničkom kontekstu.
7. **Srednji:** kompletan pixel-perfect i sigurnosni audit nisu dokazani Windows buildom.
8. **Nisko:** lokalni log pohranjuje nazive registriranih ključeva i vrijeme događaja.

## Dead code

Pregledom glavnih WPF callbackova, usluga i modela nisu uvedeni nedovršeni lažni prijavni pozivi. Dovršeni su događaji povezani s novim početnim vodičem. Potpuna statička analiza svih putanja izvršavanja i neovisni dead-code audit još nisu provedeni i ne treba ih označavati dovršenima.

## Provjere prije produkcijske upotrebe

Potrebni su fizički testovi s najmanje tri USB uređaja, provjera odspajanja i povratnog spajanja, Windows oporavka, DPI vrijednosti 100–200%, pristupačnosti, cjelovita sigurnosna analiza, potpisivanje izdavanja i eventualna zasebna revizija prijavne integracije.

**Status: razvojna alfa, nije potvrđena kao produkcijski sigurna.**
