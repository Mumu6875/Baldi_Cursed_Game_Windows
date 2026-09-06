# Shell düzeltmesi ve Android derleme log’u incelemesi

İncelenen dosya: `18968484226527-baldi-game-project-default-android-2.log.txt`.

## Derlenen sürüm

Log’un 77. satırındaki revision: `ec03aa5858761dc1eb6c4d40486c71e9f4c7c3bb`.
11178–11179. satırlar derlemenin başarılı olduğunu bildiriyor. Unity sürümü `6000.3.22f1`.
Bu APK, oda ID’lerini ekleyen Android `43e3108` / Windows `06d9717` sürümlerinden eskidir.
Eski commit’in `ShellItem.InstallPickup` metodu hâlâ `GameObject.Find("FacultyRoom1")`
kullanır. School sahnesinde bu adı taşıyan beş oda olduğundan yanlış odanın seçilmesi,
sonraki masa kontrolünün başarısız olup eşyanın hiç oluşturulmamasına yol açabilir.
Güncel kod doğrudan oda ID 3’ün kayıtlı masa bağlantısını kullanır.

## Bu değişiklikte giderilen sorunlar

1. Shell kurulumu, `GameControllerScript.Start()` içinde envanter simgesinin kaydından
   hemen sonra çalışır. Mobil arayüzün veya atmosfer kurulumunun tamamlanmasına bağlı değildir.
   Bootstrap’in ikinci çağrısında mevcut veya toplanmış Shell bulunursa yenisi üretilmez.
   Yalnızca Phase 2 / School koşulu korunur.
2. Unity’nin yeni sürümlerine uygun olarak koddan istenen yerleşik font adı
   `LegacyRuntime.ttf` olarak güncellendi. Android’de altı, Windows’ta beş çağrı değişti.
   Eski font çağrısı Shell’den önceki arayüz kurulumunu kesebilecek bir hata yoluydu;
   gönderilen dosya cihaz çalışma log’u olmadığı için bunun kullanıcının oturumunda
   gerçekleştiği kesin olarak söylenemez.
3. Masa çarpışma kutusunun `y=2,50` üst yüzeyi ile görünür modelin `y≈2,65` üst yüzeyi
   birlikte değerlendirilir. Eşyanın altı 0,05 birim açıklıkla `y≈2,70` seviyesine yerleşir.
4. Görsel 3 birim yüksekliğindedir. Yeni kutu biçimindeki toplama alanı `y≈2,70–5,70`
   aralığındadır. `y=5` kamera ışını eski küresel kapsülün tepesine teğet geçiyordu;
   artık kutunun içinden geçer. Masa üzerindeki yatay alana da sığar.

## Log’daki uyarıların tamamı ve durumları

| Tanı | Log’daki yer / adet | Durum |
| --- | --- | --- |
| C# `warning CS...` | Yok | Bu log’da proje C# derleyici uyarısı bulunmadı. |
| Clang: `'-x c++' after last input file has no effect` | 9066, 9068, 9070, 9072; dört kez | UnityClassRegistration / UnityICallRegistration derlemelerinde Unity’nin ürettiği komut satırından geliyor. Proje C# değişikliğiyle giderildiği doğrulanamadı. |
| TextMeshPro: büyük metot için ayrı C++ dosyası oluşturulması | Üç farklı metot, iki mimari; 9076–9078 ve 9120–9122. Sonuç bölümünde aynı üç tanı yeniden yazılıyor. | Unity IL2CPP kod üretiminin uyarısı. Unity’nin hata takip sisteminde aynı tanı kayıtlı. Bu depoda motor/paket için doğrulanmış bir düzeltme bulunmadığından devam edebilir. |
| `[W] FindFirstFile() failed: ...cpp\Symbols` | 9048 ve 9112 | Unity/Bee sembol klasörü tarama tanısı; devamındaki derleme başarılı. Bu ortamda araç zinciri düzeltmesi doğrulanamadı. |
| `[warning]` olarak işaretlenmiş `Warnings` klasörü içe aktarma/boyut satırları | 3918, 4327, 10095 | Gerçek uyarı değil; dosya yolu içinde geçen kelimeyi log sunumu işaretliyor. |
| `[error] ...LogAssemblyErrors` / `PrintStdoutOnErrorProcessor` | Zamanlama ve çağrı yığını satırları | Tek başına derleme hatası değil. İlgili asıl tanılar yukarıdaki araç zinciri uyarıları; sonuç başarılı. |

Araç zinciri uyarıları giderilmiş veya sıfırlanmış olarak raporlanmamıştır.
Uyarı filtreleme, derleyici uyarılarını susturma, paket kaynaklarını değiştirme veya
doğrulanmamış Unity sürüm yükseltmesi uygulanmamıştır. Sonraki gerçek Unity derlemesinde
bu tanıların tekrar değerlendirilmesi gerekir.

## Yayın öncesi yapılan kontroller

- İki School sahnesinin gerçek dönüşüm zincirleri üzerinden masa, model ve kamera
  koordinatları hesaplandı: beş benzersiz oda ID’si; ID 3 doğru, etkin masaya bağlı.
- `python Tools/verify_shell_geometry.py` ile yüzey açıklığı, masaya sığma, kamera
  yüksekliği ve sahnede fazladan kayıtlı Shell bulunmaması kontrol edildi.
- Değişen tüm C# dosyaları C# sözdizimi ayrıştırıcısıyla kontrol edildi.
- Ortak Shell dosyalarının platformlar arasında eşitliği ve platforma özgü kodun
  korunması kontrol edildi. `git diff --check` geçti.
- PNG, WAV ve School sahnesi değiştirilmedi.

`FacultyRoomBuildValidation` gerçek Shell üretim metoduyla geçici nesne oluşturup
görseli ve kamera yüksekliğinden dört etkileşim ışınını denetleyecek şekilde genişletildi.
Geçici nesne `finally` içinde silinir; sahneye fazladan Shell kaydedilmez.

Bu ortamda Unity Editor ve cihaz bulunmadığı için yeni APK/EXE derlemesi, Unity içindeki
yeni kontrol ve canlı oynanış testi çalıştırılmadı. Sözdizimi ve geometrik kontroller
tam Unity derlemesi veya oynanış testi yerine geçmez. Sonraki derlemede bu commit’in
alındığı, Phase 2’de bir Shell oluştuğu, toplanabildiği ve yeniden oluşmadığı doğrulanmalıdır.

## Resmî kaynaklar

- [Unity: yerleşik font adının LegacyRuntime.ttf olarak değiştirilmesi](https://docs.unity.com/en-us/grow/ads/changelog)
- [Unity: TextMeshPro / IL2CPP büyük metot uyarısı kaydı](https://issuetracker.unity.com/issues/22645/textmeshpro-warnings-are-thrown-after-building-ar-mobile-vr-and-mixed-reality-projects)
