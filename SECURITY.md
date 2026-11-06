# Sigurnosni model — Brendigo USB Key 0.2

## Granice sustava

U ovoj verziji ne postoji komponenta koja može otključati Windows logon. Ne pohranjuju se Windows lozinke, Windows Hello PIN-ovi, hash lozinke sustava ili podaci za zaobilaženje prijave. Aplikacija ne mijenja Windowsove pružatelje vjerodajnica.

## Odnos prijetnji i zaštita

| Prijetnja | Stanje |
| --- | --- |
| Izgubljen registrirani stick | Opoziv identiteta u aplikaciji na računalu |
| Kopirana datoteka na običnom sticku | **Nije zaštićeno**; zahtijeva hardverski izazov-odgovor |
| Napadač s pristupom otključanoj Windows sesiji | **Nije riješeno**; Windows je granica povjerenja |
| Čitanje lokalnog registra drugim Windows računom | DPAPI CurrentUser štiti registar |
| Kratki prekid USB detekcije | Tri uzastopna neuspjela skeniranja prije auto-locka |
| Gašenje procesa / restart računala | Auto-lock nije stalno aktivna zaštita |
| Gubitak svih USB-ova | Windows lozinka/Hello ostaju neizmijenjeni |
| Gubitak BitLocker ključa | Izvan opsega; BitLocker recovery key mora biti sigurno spremljen |

### Lokalni materijal

- USB: .brendigo-usb-key/key.json sa zasebnim 32-bajtnim nasumičnim tokenom po uređaju.
- Windows: %LOCALAPPDATA%\Brendigo\USBKey\registry.dat, enkriptiran Windows DPAPI CurrentUser mehanizmom.
- Lokalni zapisnik: events.log, s vremenima i nazivima ključeva, bez tokena i lozinki.
- Aplikacija ne koristi cloud servise i nema telemetriju.

**UPOZORENJE:** Skrivanje datoteke na USB-u nije enkripcija niti zaštita od kopiranja. Obični USB nije dovoljan za sigurnosno kritičnu provjeru identiteta.

### Pravila za budući Credential Provider

- Ne instalirati prijavni dodatak dok nije napravljen i testiran sigurni recovery put.
- Windowsov zadani način prijave mora ostati aktivan.
- Ne zadržavati niti automatski upisivati korisničku Windows lozinku.
- Koristiti podržan način prijave i neovisnu reviziju koda.
- Za FIDO2 uređaje provjeriti podršku za konkretan tip Windows korisničkog računa.

### Prijava ranjivosti

Ne objavljuj ranjivosti s tokenima ili osobnim podacima u javnim issues. Kontakt: https://brendigo.com
