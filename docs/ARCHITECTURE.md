# Arhitektura

## Verzija 0.2

USB #1, USB #2, USB #3 → Brendigo WPF aplikacija → UsbKeyManager.

UsbKeyManager provjerava lokalni registar enkriptiran DPAPI-jem. Opcionalno poziva LockWorkStation za zaključavanje *već otvorene* Windows sesije.

- **SplashWindow**: četiri uvodna koraka i sigurnosno objašnjenje, bez izmjene Windows prijave.
- **MainWindow**: WPF upravljanje, prikaz i opcije.
- **TokenCodec**: zaseban strogo validiran format nasumičnog USB tokena i SHA-256 dokaza.
- **UsbKeyManager**: tokeni po sticku, SHA-256 dokaz, lokalni popis povjerenja, praćenje, opoziv, zapisnik i opcionalno zaključavanje.
- **StartupService**: HKCU Run uz argument --autostart, koji preskače uvod i odmah uključuje nadzor nakon Windows prijave.
- **TrayController**: Windows obavijesno područje s otvaranjem, ručnim zaključavanjem i izričitim izlazom; nije servis.
- **EnrollDialog**: odabir izmjenjivog USB pogona i naziva.

Datoteka na USB-u ne izvršava se automatski. Program ne zahtijeva administratorske ovlasti.

## Praćenje automatskog zaključavanja

Svake tri sekunde pregledavaju se spremni izmjenjivi pogoni. Ako se barem jedan valjani ključ pojavi, nadzor se naoružava. Kada nema nijednog valjanog USB-a u tri uzastopna skeniranja i auto-lock je omogućen, poziva se LockWorkStation(). Windows zadržava standardne metode prijave.

## Izolirani vizualni testovi i distribucija

Posebni WPF vizualni runner na Windows CI-u mjeri i renderira 12 pravih sadržaja zaslona pomoću WPF RenderTargetBitmap. Snimke se provjeravaju i spremaju u assets/screenshots, a korisniku se nude kroz marketinški README i portable paket. To nije zamjena za ručno testiranje stvarnih dijaloga na Windows monitoru.

Windows izdanje sadrži samostalni Portable.exe, ZIP s aplikacijom, USB Setup Kit (Portable.exe, HR/EN vodiči, opcionalna skripta za kopiranje) i SHA-256 sume. Skripta ne generira USB tokene niti otključava Windows.

## Planirane faze

1. Automatizirani jedinični i integracijski testovi uz fizički USB; CI i oporavak oštećenih podataka.
2. Snažnija zaštita administracijskih operacija i siguran lokalni servis za kontinuirani nadzor.
3. Podrška za namjenske kriptografske tokene s challenge-response.
4. **Zasebna sigurnosna revizija** za Windows Credential Provider, testirani fallback i kompatibilnost računa. Tek tada razvijati stvarno otključavanje Windows zaslona.

Brendigo dizajn koristi lokalne resurse i Windows WPF.
