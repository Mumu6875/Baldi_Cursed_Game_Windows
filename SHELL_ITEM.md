# Shell eşyası

Shell'in eşya kimliği **12**'dir. Önceki eşyaların kimlikleri değiştirilmemiştir: 0 boş slot, 1–11 mevcut eşyalar, 12 Shell için kullanılır. Kimlik çakışması yoktur.

## Konum ve adet

Phase2'de, School sahnesindeki Alarm Clock'un bulunduğu **FacultyRoom1** odasında, yalnızca seçili masanın üzerinde **bir adet** Shell oluşur. Zeminde veya başka odalarda Shell oluşturulmaz.

Seçili masa, odanın `Objects` nesnesinin altındaki, yerel konumu `(-4, 1, -10)` olan `Desk` nesnesidir. Mevcut School sahnesinde masanın dünya merkezi `(-45, 1, 171)` konumundadır. Çarpışma kutusunun üstü `y=2,5`, görünür masa modelinin üstü yaklaşık `y=2,65` seviyesindedir. Kurulum her iki yüzeyi de dikkate alır; Shell’in alt kenarı 0,05 birim açıklıkla `y≈2,70` seviyesine, merkezi `y≈4,20` seviyesine yerleştirilir. Görsel ve kutu biçimindeki etkileşim alanı 3 birim yüksekliğindedir; üst kenarı `y≈5,70` olur. Kamera `y=5` seviyesinde olduğundan ekran ortasından çıkan yatay ışın artık etkileşim alanının içinden geçer. Masanın yatay sınırları `x=-47…-43` ve `z=166…176` aralığındadır; 3 birimlik eşya bu yüzeye sığar. Masa Alarm Clock ve Quarter eşyalarından ayrıdır.

Fakülte odalarının ID'leri sahnede `FacultyRoomIdentity` bileşeniyle kalıcı olarak tutulur. ID'ler oda adından, konumundan veya arama sırasından çalışma anında üretilmez. Eşya ID'leri ile oda ID'leri ayrı sistemlerdir; örneğin oda ID 3 ile eşya ID 3 çakışmaz.

| Oda ID | Mevcut dünya konumu (x, y, z) | Not |
| --- | --- | --- |
| 1 | (-85, 0, 55) | Fakülte odası |
| 2 | (95, 0, 85) | Fakülte odası |
| 3 | (-55, 0, 175) | Alarm Clock ve Shell'in bulunduğu oda |
| 4 | (-15, 0, 85) | Fakülte odası |
| 5 | (85, 0, 245) | Fakülte odası |

Konumlar yalnızca odaları tanımak içindir. Runtime oda seçimi ID ile yapılır. Shell, oda ID 3'ün sahnede kayıtlı `ItemTable` bağlantısını kullanır. Böylece aynı `FacultyRoom1` adını taşıyan beş odanın hangisinin ilk bulunduğuna bağımlılık kaldırılmıştır. İsim veya masa koordinatı araması yapılmaz. Eşyanın görsel malzemesi için referans alınan mevcut item sprite'ı da sahnede doğrudan bağlanmıştır.

Kurulum, toplanıp devre dışı bırakılmış Shell nesnesini de kontrol eder. Aynı sahne oturumunda kurulum tekrar çağrılsa bile eşya yeniden oluşmaz. Yeni bir oyun/sahne yüklemesinde bir adet yeniden oluşturulur. Oda ID 3 veya masa bağlantısı eksikse hata kaydı yazılır. Aynı sahnede tekrarlanan bir oda ID'si bulunursa ilk oda keyfî olarak seçilmez. `FacultyRoomBuildValidation`, derleme sırasında oda ID'lerinin pozitif ve benzersiz olduğunu, fakülte odalarının kimlik bileşenlerini ve Shell masasının doğru odaya bağlı olduğunu kontrol eder.

Kurulum `GameControllerScript.Start()` içinde, eşya simgesi kaydedilir kaydedilmez çalışır. Mobil arayüz veya atmosfer kurulumunun başarısına bağımlı değildir. Bootstrap içindeki ikinci çağrı tekrar oluşturma kontrolü sayesinde yeni eşya üretmez. Phase 1, Phase 3, Phase 4 ve TestRoom sahnesinde Shell oluşturulmaz. Başarılı kurulum oda ID’si ve dünya konumuyla log’a yazılır.

Android ve Windows'ta mevcut ekran ortası etkileşim sistemi ve envanter arayüzü kullanılır.

## Kullanım ve Baldi üzerindeki etki

Eşya yalnızca etkin, `CursedBaldiVisual` bileşenine ve geçerli bir `NavMeshAgent` bileşenine sahip Lanetli Baldi'de kullanılabilir. Normal Baldi'de veya diğer geçersiz kullanım durumlarında eşya tüketilmez.

Başarılı kullanımda Baldi'nin kafası kabukla örtülür. **10 saniyelik oyun süresi** boyunca görüşü engellenir ve başlangıçta bir kez rastgele gezinmeye yönlendirilir. İşitmesi ve ses öncelikleri değiştirilmez; sonraki sesler hedefini değiştirebilir. Oyuncuyu görerek veya doğrudan hedefleyerek takip etmesi süre dolana kadar engellenir.

Yeniden kullanım süreyi üst üste eklemek yerine tekrar 10 saniyeye ayarlar. Oyun duraklatıldığında süre ve ses durur. Baldi devre dışı bırakıldığında, sahneden çıkıldığında veya süre dolduğunda kabuk kaldırılır ve ses kesilir.

## Görsel

Envanter simgesi, masadaki eşya ve Baldi'nin kafasındaki kaplama **aynı `Assets/Resources/CursedMod/Shell.png` dosyasını** kullanır. Kafa için ayrı bir kabuk resmi üretilmemiştir; aynı görsel kafayı örtecek şekilde yatay ve dikey ölçeklenir.

Görsel, kullanıcının gönderdiği mevcut eşya resmi referans alınarak yerleşik imagegen aracıyla oluşturuldu. Üretim isteminin Türkçesi: Tek bir içi boş, kahverengi/bej, çizgili, miğfer biçiminde opak kabuk; önden çapraz görünüm ve şeffaf arka plan. Referanstaki düşük çözünürlüklü 1990'lar eşya görsellerine uygun pütürlü doku, basit ışıklandırma ve belirgin piksel kenarları. Yüz, karakter, yazı, başka eşya, zemin gölgesi, parlak modern illüstrasyon veya aşırı süsleme bulunmasın.

Kafa kaplaması, CursedBaldi görselinin normalize edilmiş kaynak koordinatlarını kullanır. Özgün 1024×1536 koordinat sisteminde merkezi `(514,165)`, genişliği 420 ve yüksekliği 540'tır. Bu hesap Unity'nin içe aktarma çözünürlüğünden bağımsızdır. Alfa analizi, kaynak görselde 315. satırın üzerindeki 42.060 opak kafa pikselinin tamamının örtüldüğünü doğruladı.

## Ses

Ses dosyası: `Assets/Resources/CursedMod/ShellUse.wav`.

Gönderilen M4A, **mono, 48 kHz, 16 bit PCM WAV** biçimine dönüştürüldü ve ses seviyesi yükseltildi. 8,96 saniyelik kayıt, 10 saniyelik etkiyi tamamlamak için döngüde çalınır.

Ses kaynağı Baldi'nin kafasını takip eder. Ses düzeyi 1, doğrusal uzaklık azalması, minimum mesafe 20 ve maksimum mesafe 250 Unity birimidir. Doppler etkisi kapalıdır. Kabuk sesi, Baldi'nin kendi konumuna gitmesine neden olmaması için yapay zekânın `Hear()` metodunu tetiklemez.

Ölçülen tepe seviyesi −1,74 dBFS, RMS seviyesi −19,51 dBFS'dir; kırpılmış örnek yoktur. Bu değerler dijital ses seviyesidir, hoparlörden çıkan fiziksel ses şiddeti değildir.

## Doğrulama ve sınırlar

İlk Shell sürümü yayınlanmadan önce değişen C# dosyalarının sözdizimi, eşya kimliği/toplama/kullanım bağlantıları, görüş ve işitme bağlantıları, duraklatma ve yeniden kullanım dahil 30/60/120 FPS süre hesabı, kafa kaplaması, WAV örnekleri, Unity meta GUID'lerinin benzersizliği ve iki platformdaki ortak dosyaların eşitliği kontrol edildi. Süre kontrolü bağımsız bir matematik modelidir; Unity oynanış testi değildir.

Masa yerleşimi güncellemesinde iki platformun School sahnesindeki oda/masa hiyerarşisi, masa yüzeyi sınırları, eşyanın yüzeye sığması, yakındaki diğer eşyalar, kimlik eşlemesi ve toplanmış eşyayı da kapsayan tekrar oluşma engeli incelendi.

Bu ortamda Unity Editor ve cihaz üzerinde canlı oynanış testi yapılamadı. Unity Build Automation, eksik veya yanlış içe aktarılmış görsel/ses dosyalarını reddetmek için `ShellBuildValidation` kontrolünü çalıştırır. Unity'de `Cursed Baldi > Validate Shell Assets` komutu da kullanılabilir.

Yeni derlemede Phase2'de masada bir adet eşya bulunması, toplandıktan sonra tekrar oluşmaması, iki platformda toplama/kullanım, farklı açılardan kafa kaplaması, duraklatma, yeniden kullanım, sürenin bitmesi, sahneden çıkış ve görüş engelliyken ses takibi canlı olarak kontrol edilmelidir. Normal Baldi'de kullanmaya çalışınca eşyanın envanterde kaldığı da doğrulanmalıdır.

## 6 Eylül 2026 yerleşim düzeltmesi

`FacultyRoomBuildValidation` artık gerçek çalışma zamanı üretim metoduyla geçici bir Shell oluşturur. Görselin etkinliğini, masa üstünde durmasını, masaya sığmasını ve kamera yüksekliğindeki dört yatay ışının etkileşim kutusuna çarpmasını kontrol eder; geçici nesneyi `finally` içinde kaldırır. Bu kontrol School sahnesi derlenirken otomatik çalışır. Açık School sahnesinde `Cursed Baldi > Validate Shell Placement In Open School` menüsünden de çalıştırılabilir.

Bu ortamda sahne dosyalarının dönüşüm/geometri hesabı ve C# sözdizimi kontrol edilmiştir. Unity derleme kontrolünün çalıştığı veya cihaz testinin geçtiği henüz iddia edilmemektedir. Gönderilen eski derleme log’unun incelemesi ve kalan araç zinciri uyarıları `BUILD_LOG_REVIEW.md` dosyasındadır.
