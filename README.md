ProfileActions v2.0
====================

Внимание!
---------

*Данная ветка не годится для использования в production из-за бага с дублированием содержимого переменных в многопотоке*: http://zennolab.com/discussion/threads/threadstatic-i-list-string-peresekajutsja-dannye-v-potokax-dazhe-parallelnyx.41807/

Выложено с целью того, чтоб коммьюнити помогло допилить либу. Скорее всего лучше переписать часть кода и сделать работу через `project.Context`.

Описание
--------

Расширенный вариант работы с профилями в ZennoPoster. Возможность сохранять не только информацию о профиле в **.zpprofile*, но и заголовки инстанса, устанавливаемые методом *instance.SetHeader*.

Библиотека сделана для исправления появившейся в 5.10.3.1 версии баги, из-за которой перезаписываются некоторые данные при сохранении профиля ([пруф баги](http://zennolab.com/discussion/threads/bagi-v-versii-5-10-3-1.34535/page-5#post-258110)). Скорее всего похожие проблемы могли быть и на других версиях, но тому нет точного подтверждения.

История версий
--------------

**v2.0 [22.02.2017]:**

По сути осталось всё тоже самое, за исключением того, что теперь не нужно думать о потоках и работе в многопотоке.
Библиотека и C# сами разруливают это, теперь работа с библиотекой стала на порядок проще и логичнее.

Изменения:

- Удален отовсюду параметр `thread_id`
- Удален генератор идентификаторов `GenerateThreadID`
- Удалена "очистка мусора" `DestroyThreadID`
- Удален параметр `destroy_thread` в методе `Save`
- Теперь свойства профиля и заголовки инстанса объявлены с `[ThreadStatic]`
- Добавлен приватный метод инициализации свойств профиля `InitProperies`
- Добавлен приватный метод инициализации заголовков инстанса `InitHeaders`

**v1.0 [09.02.2017]:**

Первая версия библиотеки


Установка
---------

1. Взять [`ProfileActions.dll`](https://github.com/lord-alfred/ProfileActions/releases) и положить в директорию: `C:\Program Files (x86)\ZennoLab\RU\ZennoPoster Pro\[версия ZP]\Progs\ExternalAssemblies`
2. Перезапустить ZennoPoster/ProjectMaker
3. В проекте выбрать `Добавить действие` -> `Свой код` -> `Ссылки из GAC`
4. Зайти в появившийся внизу блок `References` (в Расширенном редакторе), нажать кнопку `Добавить...`, затем кнопку `Обзор...`
5. В появившемся окне выбрать: `C:\Program Files (x86)\ZennoLab\RU\ZennoPoster Pro\[версия ZP]\Progs\ExternalAssemblies\ProfileActions.dll`
6. В проекте выбрать `Добавить действие` -> `Свой код` -> `Директивы using и общий код`
7. Зайти в появившийся внизу блок `OwnCodeUsings` (в Расширенном редакторе) и в окне "Директивы Using" вставить:
> using ZPProfileActions;

 После этого у вас в код появится класс `ProfileActions` с публичными методами, описанными ниже.

Методы ProfileActions
---------------------

*Для быстрого понимания - в репозитории расположен тестовый проект `test_project.xmlz`.*

 - **Установка значения свойства профиля**

> void SetProperty(IZennoPosterProjectModel project, string propname, string value)

Список свойств профиля, которые можно устанавливать - можно взять отсюда: [Profile Public Properties](https://help.zennolab.com/en/v5/zennoposter/5.10.3/topic854.html).

*Параметры:*

	project - переменная проекта
	propname - имя свойства
	value - значение свойства

*Пример использования:*

> ProfileActions.SetProperty(project, "UserAgent", "Firefox");

 - **Получение значения свойства профиля**

> string GetProperty(string propname)

*Параметры:*

	propname - имя свойства

*Пример использования (переменная `result` должна быть создана заранее):*

> project.Variables["result"].Value = ProfileActions.GetProperty("UserAgent");

 - **Установка значения заголовка инстанса**

> void SetHeader(Instance instance, string headername, string value, bool is_navigator_field=true)

Расширенный вариант стандартного метода [instance.SetHeader](https://help.zennolab.com/en/v5/zennoposter/5.10.3/webframe.html#topic246.html), все устанавливаемые значения сохраняются в профиль.

Список заголовков инстанса, которые можно устанавливать - можно взять отсюда: [NavigatorField Members](https://help.zennolab.com/en/v5/zennoposter/5.10.3/topic630.html).

*Примечание:* согласно стандартным ограничениям метода *instance.SetHeader* - текущий метод нужно вызывать перед вызовом любого другого метода объекта *Instance*.


*Параметры:*

	instance - переменная инстанса
	headername - заголовок инстанса (название поля ZennoLab.InterfacesLibrary.Enums.Browser.NavigatorField или просто HTTP-заголовок)
	value - значение
	is_navigator_field - true при установке поля ZennoLab.InterfacesLibrary.Enums.Browser.NavigatorField или false при установке HTTP-заголовка

*Примеры использования:*

> ProfileActions.SetHeader(instance, "Language", "ru");
> ProfileActions.SetHeader(instance, "HTTP_USER_AGENT", "ZennoPoster", false);

 - **Сохранение профиля со свойствами и заголовками инстанса**

> void Save(IZennoPosterProjectModel project, string path, bool saveProxy=false, bool savePlugins=false, bool saveLocalStorage=false, bool saveTimezone=false, bool  saveGeoposition=false)

*Параметры:*

	project - переменная проекта
	path - полный путь к файлу профиля (вместе с расширением)
	saveProxy - сохранять прокси
	savePlugins - сохранять список плагинов
	saveLocalStorage - сохранять содержимое localStorage
	saveTimezone - сохранять информацию о таймзоне
	saveGeoposition - сохранять геопозицию

*Пример использования:*

> string path = Path.Combine(project.Directory, "profiles", "test_profile.zpprofile");
> ProfileActions.Save(project, path, true, true, true, true, true);

 - **Загрузка профиля с простановкой свойств и заголовков инстанса**

> void Load(IZennoPosterProjectModel project, Instance instance, string path)

*Параметры:*

	project - переменная проекта
	instance - переменная инстанса
	path - полный путь к файлу профиля (вместе с расширением)

*Пример использования:*

> string path = Path.Combine(project.Directory, "profiles", "test_profile.zpprofile");
> ProfileActions.Load(project, instance, path);

Контрибьютеры
-------------

 - [DmitryAk](http://zennolab.com/discussion/members/dmitryak.17393/), спасибо за помощь с рефлекшенами и ThreadStatic
 - тут может быть твой ник и ссылка на тебя ;-)

Лицензия
--------

[CC BY-NC-SA 3.0](https://creativecommons.org/licenses/by-nc-sa/3.0/deed.ru) (Creative Commons — «Attribution-NonCommercial-ShareAlike» 3.0)

Лицензия «С указанием авторства — Некоммерческая — С сохранением условий»

Даная лицензия позволяет другим людям редактировать, поправлять и брать произведение за основу для производных в некоммерческих целях при условии, что они указывают авторство и лицензируют свои новые произведения на тех же условиях.