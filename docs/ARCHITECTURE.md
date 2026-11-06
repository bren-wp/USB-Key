# Arhitektura

## Verzija 0.1

USB #1, USB #2, USB #3 → Brendigo WPF aplikacija → UsbKeyManager.

UsbKeyManager provjerava lokalni registar enkriptiran DPAPI-jem. Opcionalno poziva LockWorkStation za zaključavanje *već otvorene* Windows sesije.

- **MainWindow**: WPF upravljanje, prikaz i opcije.
- **UsbKeyManager**: tokeni po sticku, SHA-256 dokaz, lokalni popis povjerenja, praćenje, opoziv, zapisnik i opcionalno zaključavanje.
- **StartupService**: HKCU Run za pokretanje nakon prijave u Windows.
- **EnrollDialog**: odabir izmjenjivog USB pogona i naziva.

Datoteka na USB-u ne izvršava se automatski. Program ne zahtijeva administratorske ovlasti.

## Praćenje automatskog zaključavanja

Svake tri sekunde pregledavaju se spremni izmjenjivi pogoni. Ako se barem jedan valjani ključ pojavi, nadzor se naoružava. Kada nema nijednog valjanog USB-a u dva uzastopna skeniranja i auto-lock je omogućen, poziva se LockWorkStation(). Windows zadržava standardne metode prijave.

## Planirane faze

1. Automatizirani jedinični i integracijski testovi uz fizički USB; CI i oporavak oštećenih podataka.
2. Snažnija zaštita administracijskih operacija i siguran lokalni servis za kontinuirani nadzor.
3. Podrška za namjenske kriptografske tokene s challenge-response.
4. **Zasebna sigurnosna revizija** za Windows Credential Provider, testirani fallback i kompatibilnost računa. Tek tada razvijati stvarno otključavanje Windows zaslona.

Brendigo dizajn koristi lokalne resurse i Windows WPF.
