# Brendigo USB Key — preliminarni sigurnosni i dead-code pregled

Pregled koda i CI-a: 6. studenoga 2026. Neovisni penetracijski test, formalni audit i stvarno testiranje na nekoliko fizičkih USB stickova još nisu provedeni.

## Učinjena poboljšanja

| Rizik / problem | Mjera |
| --- | --- |
| Nevaljano kodiranje USB tokena | Provjera JSON formata i canonical Base64, točno 256 bita |
| Prevelika datoteka na izmjenjivom disku | Ograničenje parsiranja na 2048 bajtova |
| Jednostavno preusmjeravanje preko junction/symlink datoteke | Odbijanje reparse-point datoteka i mapa |
| Kratko nestajanje USB-a | Tri uzastopne pouzdane odsutne provjere; nepouzdana provjera odgađa zaključavanje |
| Pogrešan zapis o uspješnom zaključavanju | Razlikovanje prihvaćenog i odbijenog zahtjeva LockWorkStation |
| Neograničen sigurnosni dnevnik | Lokalna rotacija na približno 1 MiB |
| Neuspjeh upisa opcije auto-lock | Vraćanje prethodne vrijednosti u memoriji |
| Pogreške formata tokena | Automatizirani deterministički smoke testovi u CI-u |

## Trenutačne dodatne provjere

- **55 automatiziranih USB/sigurnosnih smoke provjera:** format tokena, zasebni slučajni ID, nekanonski Base64, pokvareni tokeni, više USB ključeva i pouzdanost trostruke provjere odsutnosti.
- **13 WPF zaslona i dodatne tri varijante vizualnog renderiranja:** ekran po ekran iz stvarnog XAML-a na Windowsu, uključujući početni zaslon, onboardinge, sve glavne sekcije i dijaloge.
- **WPF wiring provjera:** automatsko otkrivanje nepostojećih XAML Click/SelectionChanged/RequestNavigate callbackova i nepostojećih screenshot datoteka.
- **Pohrana:** lokalni registar odbija nevaljane, duplicirane ili prevelike zapise; USB token se čita uz ograničen broj bajtova.
- **Sigurnosna pogodnost:** nepravilna enumeracija USB-a ili čitanje tokena ne smiju se zamijeniti pouzdanom odsutnošću ključa.

Ovaj pregled je **ciljan, preliminaran i bez formalne penetracijske provjere**. Nije profesionalni certifikacijski pregled, puni dead-code audit niti dokaz da aplikacija štiti zaključani Windows.

## Zapažanja ove iteracije — 7. studenoga 2026.

- Dodana provjera prethodno viđenog izmjenjivog pogona: ako pogon ostane prisutan, a token se više ne može provjeriti, ne tretira se kao potvrđeno vađenje USB-a.
- USB registracija prihvaća isključivo jednostavni korijen izmjenjivog diska, a ne podmapu, mrežnu putanju ili putanju relativnu pogonu.
- Registracijska forma onemogućuje potvrdu bez odabranog pogona i valjanog naziva; ograničen je broj pohranjenih ključeva na 2048.
- Dashboard prikazuje odgođeni auto-lock i upozorenje za nepouzdano očitanje.
- Ovo je izvorno-kodni i CI pregled, bez fizičkih uređaja ili neovisnog penetracijskog testiranja. Opseg 55 provjera ne obuhvaća sve rubne slučajeve Windows datotečnog sustava i autentifikacije.

## Zaštita lokalnog upravljanja — 7. studenoga 2026.

- Dodan prvi ulaz s traženom početnom lozinkom **Brendigo** i obveznom zamjenom vlastitom lozinkom, umjesto trajne zajedničke zaporke.
- Sprema se samo lokalni provjerni zapis zaštićen Windows DPAPI-jem, PBKDF2-SHA256 (210.000 iteracija, nasumična 32-bajtna sol).
- Upravljački prozor je zaštićen pri ručnom pokretanju i otvaranju iz system traya, ali lokalni nadzor USB-a može i dalje raditi u pozadini.
- Dodani testovi pogrešne početne lozinke, inicijalizacije, promjene, trajnosti podataka, nevažeće stare lozinke i privremenog ograničenja pokušaja.
- **Rizik:** lokalnu zaštitu mogu zaobići procesi koji već imaju jednak pristup korisničkom Windows profilu i programskim datotekama. Ograničenje pokušaja je samo unutar procesa; ne postoji neovisna revizija ni hardverska prijava.
- Pravi Windows sign-in nije izmijenjen. USB otključavanje zaslona i dalje nije implementirano.

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
