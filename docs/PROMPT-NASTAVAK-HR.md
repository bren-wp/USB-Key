
# BRENDIGO USB KEY — NASTAVAK PROJEKTA U DRUGOM CHATU

Nastavi **postojeći** projekt na GitHubu: https://github.com/bren-wp/USB-Key.

PR #1 je već spojen u granu main. **Ne otvaraj nove Pull Requestove.** Nastavi izravno od najnovijeg stanja main grane, uz provjeru koda, README-a, resursa, GitHub Actions buildova i dostupnih izdanja. Nemoj ponovno započinjati projekt, brisati funkcionalnosti niti tvrditi da su promjene napravljene bez stvarnih commitova.

## 1. Pixel-perfect 1:1 izrada

Prođi redom **Splash / Welcome, First Run, Dashboard, USB Keys, Register USB Wizard, Rename, Revoke, Recovery, Security Log, Settings, About, Confirmations, Alerts, Empty/Error/Success states**. Svi zasloni moraju odgovarati dostupnim referentnim slikama: logotip, launch ikona, 3D elementi, header, sidebar, pozadine, tipografija, boje, kartice, gumbi, forme, polja, prekidači, tablice, razmaci, sjene, obrubi i poravnanja.

Radi zaslon po zaslon, na realnom Windows WPF sučelju. Provjeri svaki raspored na više veličina i DPI postavki. Testiraj sva stanja gumba (normal, hover, pressed, focus, disabled), tipkovnicu i čitač zaslona. Snimi **stvarne screenshotove aplikacije**, usporedi s referencama i dokumentiraj odstupanja. Ne podmeći AI mockup kao screenshot funkcionalne aplikacije.

**Ne generiraj slike u chatu.** U README-u mogu biti stvarne snimke i postojeći ili zakonito izrađeni lokalni grafički resursi, uz jasnu razliku između koncepata i funkcionalnih zaslona.

## 2. Funkcionalnosti

- Svaki obični USB stick zasebno registrirati; podržati neograničen praktičan broj ključeva uz sigurne resurse i provjeru.
- Svaki mora imati jedinstven token, status, naziv, opoziv i lokalni sigurnosni log.
- Obrađivati hot-plug, odspajanje, kvar, oštećeni token, promjenu slova pogona i više istodobnih USB-ova.
- Auto-lock tek kad se ukloni posljednji aktivni ključ; ne smije lažno zaključavati zbog povremenih USB grešaka.
- Sve funkcionalnosti moraju biti stvarne; bez lažnih gumba, placeholdera i razvojnih tekstova.
- Sve lokalno bez nepotrebnih vanjskih resursa i telemetrije.

## 3. Sigurnost i oporavak

Trenutačno je podržana samo lokalna registracija i zaključavanje otvorene sesije. **Windows sign-in unlock putem USB-a još nije napravljen.** Nemoj prikazivati tu funkciju kao dostupnu. Nikad ne spremaj korisničku Windows lozinku ni Hello PIN i ne zaobilazi autentifikaciju. Windowsova uobičajena prijava mora ostati operativna. BitLocker oporavak je zaseban. Obični USB token može se kopirati. FIDO2 hardversku autentifikaciju i Credential Provider razmatraj samo uz zaseban sigurnosni dizajn, testirani fallback i sigurnosnu reviziju.

## 4. Windows izdanje

Napravi i provjeri na CI-u:
- Brendigo-USB-Key-Portable.exe (samostalni, nepotpisani Windows x64 **testni** program);
- Brendigo-USB-Key-win-x64-portable.zip;
- SHA256SUMS.txt;
- USB komplet s uputama HR/EN, višestrukom registracijom, oporavkom, FAQ-om i troubleshootingom.

Ako mijenjaš naziv executablea, provjeri StartupService i sve reference. Izradi stvarni GitHub Release **samo ako su datoteke doista objavljene i provjerene**; inače jasno navedi da postoji samo Actions artifact. Novi release za svaku uspješnu iteraciju, bez otvaranja novog PR-a, kad je to tehnički omogućeno.

## 5. README i provjere

Profesionalan marketinški README s lokalnim ikonama, bannerom, stvarnim screenshotovima kada postoje, tablicom funkcionalnosti, jasnim ograničenjima, uputama za 2–3 USB sticka, download sekcijom, instalacijom, sigurnošću, changelogom i poveznicama.

Pokreni build, provjeru kvalitete, sigurnosni i dead-code audit, i testove USB ponašanja na Windowsu. Jasno odvoji automatski CI test od fizičkih hardverskih testova koji nisu provedeni. Ostavi autora **Brendigo**, https://brendigo.com, MIT licencu i postojeći repozitorij netaknutima osim potrebnih poboljšanja.

**Odmah kreni implementirati sljedeći ekran i korigirati paket; ne traži ponovno potvrdu. Na kraju ispiši konkretne commitove, CI rezultat, stvarne artefakte i preostale rizike.**
