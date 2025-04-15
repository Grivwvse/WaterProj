// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

if (window.location.pathname.includes("/ConsumerAccount") || window.location.pathname.includes("/TransporterAccount"))
{
    const editButton = document.getElementById('editButton');
    const nameInput = document.getElementById('name');
    const loginInput = document.getElementById('login');
    const form = document.getElementById('editForm');

    let editing = false;

    editButton.addEventListener('click', () => {
        editing = !editing;
        nameInput.readOnly = !editing;
        loginInput.readOnly = !editing;

        if (editing) {
            editButton.textContent = 'Сохранить';
            editButton.classList.remove('btn-primary');
            editButton.classList.add('btn-success');

        }
        else {
            // Отправляем форму
            form.submit();
        }
    })
}



document.addEventListener("DOMContentLoaded", function () {
    if (!window.location.pathname.includes("/CreateRoute")) return;

    ymaps.ready(init);

    let selectedPlacemark;
    const stopsData = [];
    const polylines = [];
    const mapData = {
        stops: [],  // Содержит остановки с их координатами и свойствами
        lines: []   // Содержит линии маршрута, включая промежуточные точки
    };

    function init() {
        const myMap = new ymaps.Map('myMap', {
            center: [59.938339, 30.313558],
            zoom: 10
        });

        const placemarkCollection = new ymaps.GeoObjectCollection();
        let routePolyline = null;

        function getMarksCoords(collection) {
            const coordsArray = [];
            collection.each(obj => coordsArray.push(obj.geometry.getCoordinates()));
            return coordsArray;
        }

        // Функция для создания и добавления полилинии
        function createRoutePolyline(coords) {
            // Удаляем предыдущую линию, если она существует
            if (routePolyline) {
                myMap.geoObjects.remove(routePolyline);
            }

            routePolyline = new ymaps.Polyline(coords, {}, {
                strokeColor: '#FF0000',
                strokeWidth: 4,
                strokeOpacity: 0.6,
                editorDrawingCursor: "crosshair",
                editorMaxPoints: 100,  // Максимальное количество точек в полилинии
                draggable: false
            });

            myMap.geoObjects.add(routePolyline);

            // Добавляем редактор полилинии
            routePolyline.editor.startEditing();

            // Обработчик события изменения геометрии полилинии
            routePolyline.geometry.events.add('change', function () {
                // Получаем все координаты полилинии, включая промежуточные точки
                const lineCoordinates = routePolyline.geometry.getCoordinates();

                // Логируем количество точек для отладки
                console.log("Точек в линии после изменения:", lineCoordinates.length);

                // Обновляем данные линии в mapData
                mapData.lines = lineCoordinates;
            });

            return routePolyline;
        }

        myMap.events.add('click', function (e) {
            const coords = e.get('coords');
            const placemark = new ymaps.Placemark(coords, {
                iconContent: 'Новая остановка',
                balloonContentHeader: 'Остановка маршрута',
                balloonContentBody: 'Здесь будет описание',
                hintContent: 'Новая остановка',
                balloonContent: `Координаты: ${coords[0].toFixed(6)}, ${coords[1].toFixed(6)}`
            }, {
                preset: 'islands#blueStretchyIcon',
                draggable: true
            });

            placemarkCollection.add(placemark);
            myMap.geoObjects.add(placemarkCollection);

            placemark.events.add('click', () => {
                selectedPlacemark = placemark;
                document.getElementById('coords').value = placemark.geometry.getCoordinates().join(', ');
                document.getElementById('hint').value = placemark.properties.get('hintContent');
                document.getElementById('balloon').value = placemark.properties.get('balloonContent');
                document.getElementById('iconContent').value = placemark.properties.get('iconContent') || '';
            });

            // Перерисовка маршрута с использованием новой функции
            const marksCoords = getMarksCoords(placemarkCollection);

            // Если ещё нет полилинии или нужно создать новую
            if (!routePolyline) {
                routePolyline = createRoutePolyline(marksCoords);
            } else {
                // Обновляем координаты существующей линии, учитывая промежуточные точки
                updateRouteGeometry(marksCoords, routePolyline.geometry.getCoordinates());
            }

            // Обновляем mapData.lines после обновления полилинии
            if (routePolyline) {
                mapData.lines = routePolyline.geometry.getCoordinates();
                console.log("Количество точек в линии после клика:", mapData.lines.length);
            }
        });

        // Обработка кнопки "Сохранить остановку"
        document.getElementById('saveStop').addEventListener('click', () => {
            if (!selectedPlacemark) {
                alert("Сначала выберите метку!");
                return;
            }

            const currentIconContent = selectedPlacemark.properties.get("iconContent");

            // Условие: если метка уже обновлена (т.е. имя не "Новая остановка") — запретить сохранение
            if (currentIconContent !== "Новая остановка") {
                alert("Эта остановка уже сохранена. Вы не можете её изменить.");
                return;
            }

            const iconContent = document.getElementById('iconContent').value;
            const hint = document.getElementById('hint').value;
            const balloon = document.getElementById('balloon').value;
            const coords = selectedPlacemark.geometry.getCoordinates();

            // Сохраняем текущие координаты полилинии перед изменением
            const currentLineCoords = routePolyline ? routePolyline.geometry.getCoordinates() : [];
            console.log("Текущие точки линии перед редактированием:", currentLineCoords.length);

            // Удаляем старую метку ТОЛЬКО из коллекции
            placemarkCollection.remove(selectedPlacemark);

            // Создаём новую метку
            const newPlacemark = new ymaps.Placemark(coords, {
                iconContent: iconContent,
                hintContent: hint,
                balloonContentHeader: 'Остановка маршрута',
                balloonContentBody: `${balloon}`,
                balloonContent: `Координаты: ${coords[0].toFixed(6)}, ${coords[1].toFixed(6)}<br>
                ${balloon}`
            }, {
                preset: 'islands#blueStretchyIcon',
                draggable: true
            });

            // Назначаем обработчик клика
            newPlacemark.events.add('click', () => {
                selectedPlacemark = newPlacemark;
                document.getElementById('coords').value = coords.join(', ');
                document.getElementById('hint').value = hint;
                document.getElementById('balloon').value = balloon;
                document.getElementById('iconContent').value = iconContent;
            });

            // Добавляем в коллекцию и на карту
            placemarkCollection.add(newPlacemark);
            selectedPlacemark = newPlacemark;

            // Получаем координаты всех меток (остановок)
            const marksCoords = getMarksCoords(placemarkCollection);

            // Перерисовываем маршрут, сохраняя промежуточные точки
            updateRouteGeometry(marksCoords, currentLineCoords);

            // Сохраняем в массив
            const stop = {
                Name: iconContent,
                Latitude: coords[0],
                Longitude: coords[1],
                Hint: hint,
                Balloon: balloon,
            };

            stopsData.push(stop);
            console.log("Остановка обновлена:", stop);
            alert("Остановка сохранена. Всего: " + stopsData.length);

            // Обновляем карту с новыми данными
            mapData.stops = stopsData;  // Обновляем список остановок на карте

            // Обновляем линии из текущей полилинии со всеми промежуточными точками
            if (routePolyline) {
                mapData.lines = routePolyline.geometry.getCoordinates();
                console.log("Количество точек в линии после сохранения остановки:", mapData.lines.length);
            }
        });

        // Функция для обновления геометрии маршрута с учетом новых меток, сохраняя промежуточные точки
        function updateRouteGeometry(marksCoords, lineCoordinates) {
            // Проверяем наличие координат
            if (!marksCoords || marksCoords.length === 0) {
                console.warn("Нет координат меток для обновления геометрии.");
                return;
            }

            console.log("Обновление геометрии с", marksCoords.length, "метками и",
                lineCoordinates ? lineCoordinates.length : 0, "точками линии");

            // Удаляем старую полилинию
            if (routePolyline) {
                myMap.geoObjects.remove(routePolyline);
                routePolyline = null;
            }

            // Если это первая линия или нет данных о предыдущей геометрии
            if (!lineCoordinates || lineCoordinates.length === 0 || marksCoords.length <= 1) {
                routePolyline = createRoutePolyline(marksCoords);
                return;
            }

            // Тут основная логика для сохранения формы линии
            const enhancedCoords = preserveLineShapeWithStops(marksCoords, lineCoordinates);

            // Создаем новую полилинию с сохранением формы линии
            routePolyline = createRoutePolyline(enhancedCoords);
        }

        // Функция для объединения координат меток с существующими координатами линии
        function preserveLineShapeWithStops(marksCoords, lineCoordinates) {
            // Каждая точка марки должна быть включена в конечный результат
            // Между марками нужно сохранить форму линии с промежуточными точками

            // Если линии нет или она содержит всего 2 точки (прямая линия)
            // то просто возвращаем координаты остановок
            if (!lineCoordinates || lineCoordinates.length <= 2) {
                return marksCoords;
            }

            // Проверяем совпадение первого и последнего маркера с существующими точками линии
            // Если они совпадают, то можно считать, что мы просто добавляем промежуточную точку,
            // не меняя начало и конец маршрута

            // Этот алгоритм очень простой для демонстрации:
            // Если у нас только 2 маркера, мы используем все точки существующей линии
            if (marksCoords.length === 2) {
                return lineCoordinates;
            }

            // В более сложных случаях мы соединяем ближайшие точки линии с марками
            // Создаем новый массив, начиная с первого маркера
            const result = [marksCoords[0]];

            // Для каждой пары последовательных маркеров
            for (let i = 0; i < marksCoords.length - 1; i++) {
                const currentMark = marksCoords[i];
                const nextMark = marksCoords[i + 1];

                // Находим ближайшие точки в существующей линии к текущему и следующему маркеру
                const currentIndex = findNearestPointIndex(lineCoordinates, currentMark);
                const nextIndex = findNearestPointIndex(lineCoordinates, nextMark);

                // Если оба индекса найдены
                if (currentIndex !== -1 && nextIndex !== -1) {
                    // Определяем направление движения по линии
                    const step = currentIndex < nextIndex ? 1 : -1;

                    // Добавляем промежуточные точки, если они есть
                    for (let j = currentIndex + step; j !== nextIndex; j += step) {
                        if (j >= 0 && j < lineCoordinates.length) {
                            result.push(lineCoordinates[j]);
                        }
                    }
                }

                // Добавляем следующий маркер
                result.push(nextMark);
            }

            console.log("Результат объединения:", result.length, "точек");
            return result;
        }

        // Функция для нахождения индекса ближайшей точки линии к указанной точке
        function findNearestPointIndex(points, targetPoint) {
            let minDistance = Number.MAX_VALUE;
            let minIndex = -1;

            for (let i = 0; i < points.length; i++) {
                const distance = getDistance(points[i], targetPoint);
                if (distance < minDistance) {
                    minDistance = distance;
                    minIndex = i;
                }
            }

            return minIndex;
        }

        // Функция для расчета расстояния между двумя точками
        function getDistance(point1, point2) {
            const dx = point1[0] - point2[0];
            const dy = point1[1] - point2[1];
            return Math.sqrt(dx * dx + dy * dy);
        }

        // Сохранение маршрута
        document.getElementById('saveRoute').addEventListener('click', async () => {
            const name = document.getElementById('routeName').value.trim();
            const description = document.getElementById('routeDescription').value.trim();
            const scheduleDescription = document.getElementById('scheduleDescription').value.trim();

            if (!name || stopsData.length === 0) {
                alert("Введите название маршрута и добавьте хотя бы одну остановку!");
                return;
            }

            // Завершаем редактирование полилинии перед сохранением
            if (routePolyline && routePolyline.editor.state.get('editing')) {
                routePolyline.editor.stopEditing();

                // Обновляем данные линий в mapData
                mapData.lines = routePolyline.geometry.getCoordinates();
                console.log("Количество точек в линии перед сохранением:", mapData.lines.length);
            }

            const routeData = {
                Name: name,
                Description: description,
                Schedule: scheduleDescription,
                ShipId: 1,  // ID корабля (здесь можно подставить актуальное значение)
                Stops: stopsData,
                Polyline: mapData.lines, // Сохраняем все точки полилинии, включая промежуточные
                Map: JSON.stringify(mapData)  // Карта с остановками и линиями
            };

            // Выводим содержимое объекта для отладки
            console.log('Отправляемые данные на сервер:', JSON.stringify(routeData, null, 2));

            // Создаем FormData для отправки маршрута и файлов
            const formData = new FormData();
            // Добавляем данные маршрута
            formData.append('routeData', JSON.stringify(routeData));

            // Получаем изображения
            const imageInput = document.getElementById('images');
            for (let i = 0; i < imageInput.files.length; i++) {
                formData.append("Images", imageInput.files[i]);
            }

            // Отправляем данные на сервер
            try {
                const response = await fetch('/Route/SaveRoute', {
                    method: 'POST',
                    body: formData
                });

                if (!response.ok) {
                    throw new Error(`HTTP ошибка: ${response.status}`);
                }

                const result = await response.json();
                if (result.success) {
                    alert("Маршрут успешно сохранён!");
                    // Можно добавить перенаправление на страницу маршрута
                } else {
                    alert("Ошибка при сохранении маршрута: " + (result.message || "неизвестная ошибка"));
                    console.error(result.error);
                }
            } catch (error) {
                alert("Произошла ошибка при отправке данных: " + error.message);
                console.error("Ошибка:", error);
            }
        });
    }
});

// Добавим обработчик для страницы детального просмотра маршрута
document.addEventListener("DOMContentLoaded", function () {
    // Проверяем, находимся ли мы на странице деталей маршрута
    if (!window.location.pathname.includes("/Route/RouteDetails")) return;

    // Проверяем наличие контейнера для карты
    const mapContainer = document.getElementById('routeMap');
    if (!mapContainer) {
        console.error("Контейнер для карты не найден на странице");
        return;
    }

    // Получаем ID маршрута из query-параметров URL
    const urlParams = new URLSearchParams(window.location.search);
    const routeId = urlParams.get('routeID');

    if (!routeId) {
        console.error("ID маршрута не найден в параметрах запроса");
        mapContainer.innerHTML = "<p>Ошибка: ID маршрута не указан</p>";
        return;
    }

    console.log("Загрузка карты для маршрута с ID:", routeId);

    // Инициализируем карту, когда API Яндекс.Карт загружен
    ymaps.ready(function () {
        // Загружаем данные маршрута с сервера
        fetch(`/Route/GetRouteMapData?id=${routeId}`)
            .then(response => {
                if (!response.ok) {
                    throw new Error(`Ошибка HTTP: ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                if (!data.success) {
                    throw new Error(data.error || "Неизвестная ошибка при загрузке данных карты");
                }

                // Логируем полученные данные для отладки
                console.log("Полученные данные карты:", data);

                // Инициализируем карту с полученными данными
                initRouteMap(data);
            })
            .catch(error => {
                console.error("Ошибка при загрузке данных карты:", error);
                mapContainer.innerHTML = `<p>Ошибка при загрузке карты: ${error.message}</p>`;
            });
    });

    function initRouteMap(routeData) {
        try {
            // Парсим данные карты из JSON
            let mapData;
            try {
                console.log("Строка данных карты:", routeData.map);
                mapData = JSON.parse(routeData.map);

                // Логируем данные для отладки
                console.log("Распаршенные данные карты:", mapData);
                console.log("Количество остановок:", mapData.stops ? mapData.stops.length : 0);
                console.log("Количество точек в линии:", mapData.lines ? mapData.lines.length : 0);

                if (mapData.lines && mapData.lines.length > 0) {
                    console.log("Первая точка линии:", mapData.lines[0]);
                    console.log("Последняя точка линии:", mapData.lines[mapData.lines.length - 1]);
                }
            } catch (e) {
                console.error("Ошибка при парсинге данных карты:", e);
                mapData = { stops: [], lines: [] };
            }

            // Определяем начальные координаты для карты
            const initialCoords = mapData.stops && mapData.stops.length > 0
                ? [mapData.stops[0].Latitude, mapData.stops[0].Longitude]
                : [59.938339, 30.313558]; // Центр СПб по умолчанию

            // Создаем карту
            const myMap = new ymaps.Map('routeMap', {
                center: initialCoords,
                zoom: 12,
                controls: ['zoomControl', 'typeSelector']
            });

            // Создаем коллекцию для меток
            const placemarkCollection = new ymaps.GeoObjectCollection();

            // Добавляем остановки на карту
            if (mapData.stops && mapData.stops.length > 0) {
                mapData.stops.forEach(stop => {
                    const placemark = new ymaps.Placemark([stop.Latitude, stop.Longitude], {
                        iconContent: stop.Name,
                        hintContent: stop.Hint || stop.Name,
                        balloonContentHeader: 'Остановка маршрута',
                        balloonContent: stop.Balloon || `Координаты: ${stop.Latitude.toFixed(6)}, ${stop.Longitude.toFixed(6)}`
                    }, {
                        preset: 'islands#blueStretchyIcon'
                    });

                    placemarkCollection.add(placemark);
                });
            }

            // Добавляем линии маршрута
            if (mapData.lines && mapData.lines.length > 0) {
                const polyline = new ymaps.Polyline(mapData.lines, {}, {
                    strokeColor: '#FF0000',
                    strokeWidth: 4,
                    strokeOpacity: 0.6
                });
                myMap.geoObjects.add(polyline);
            }

            // Добавляем коллекцию меток на карту
            myMap.geoObjects.add(placemarkCollection);

            // Устанавливаем границы карты, чтобы видеть весь маршрут
            if (placemarkCollection.getLength() > 0) {
                myMap.setBounds(placemarkCollection.getBounds(), {
                    checkZoomRange: true,
                    zoomMargin: 30
                });
            }
        } catch (error) {
            console.error("Ошибка при инициализации карты:", error);
            document.getElementById('routeMap').innerHTML =
                `<p>Произошла ошибка при отображении карты: ${error.message}</p>`;
        }
    }
});


//document.addEventListener("DOMContentLoaded", function () {
//    // Проверяем, находимся ли мы на главной странице
//    if (window.location.pathname != "/") return;

//    console.log("Запуск скрипта поиска и отображения карты на главной странице");

//    // Проверяем наличие элементов для поиска и карты
//    const mapContainer = document.getElementById('searchMap');
//    const searchForm = document.getElementById('routeSearchForm');

//    if (!mapContainer) {
//        console.error("Контейнер для карты не найден на странице");
//        return;
//    }

//    // Инициализируем карту, когда API Яндекс.Карт загружен
//    ymaps.ready(function () {
//        console.log("API Яндекс.Карт загружен");

//        // Создаем карту
//        const myMap = new ymaps.Map('searchMap', {
//            center: [59.938339, 30.313558], // Центр Санкт-Петербурга
//            zoom: 10,
//            controls: ['zoomControl', 'typeSelector']
//        });

//        console.log("Карта создана");

//        // Создаем коллекцию для меток остановок
//        const stopsCollection = new ymaps.GeoObjectCollection();

//        // Загружаем все остановки и добавляем их на карту
//        loadStopsAndRoutes();

//        // Инициализируем поиск, если элемент формы найден
//        if (searchForm) {
//            initSearch();
//        }

//        // Функция загрузки всех остановок и маршрутов
//        async function loadStopsAndRoutes() {
//            try {
//                console.log("Загрузка остановок...");
//                const response = await fetch('/Route/GetAllStops');

//                if (!response.ok) {
//                    throw new Error(`HTTP ошибка: ${response.status}`);
//                }

//                const data = await response.json();
//                console.log("Данные с сервера:", data);

//                // Проверяем структуру данных
//                if (!data || !data.success) {
//                    console.error("Неверный формат ответа");
//                    return;
//                }

//                const stops = data.stops || [];
//                console.log(`Получено ${stops.length} остановок`);

//                // Добавляем остановки на карту и в выпадающие списки
//                addStopsToMap(stops);
//                populateStopsDropdowns(stops);
//            } catch (error) {
//                console.error("Ошибка при загрузке остановок:", error);
//            }
//        }

//        // Функция добавления остановок на карту
//        function addStopsToMap(stops) {
//            // Очищаем текущие метки
//            stopsCollection.removeAll();

//            stops.forEach((stop, index) => {
//                if (stop.latitude !== undefined && stop.longitude !== undefined) {
//                    const placemark = new ymaps.Placemark(
//                        [stop.latitude, stop.longitude],
//                        {
//                            iconContent: stop.name || `Остановка ${index + 1}`,
//                            hintContent: stop.name || `Остановка ${index + 1}`,
//                            balloonContentHeader: 'Остановка',
//                            balloonContent: `ID: ${stop.stopId}, Координаты: ${stop.latitude.toFixed(6)}, ${stop.longitude.toFixed(6)}`,
//                            stopId: stop.stopId // Добавляем ID остановки для поиска
//                        },
//                        {
//                            preset: 'islands#blueStretchyIcon'
//                        }
//                    );

//                    // Обработчик клика по метке
//                    placemark.events.add('click', function (e) {
//                        const stopId = placemark.properties.get('stopId');
//                        const stopName = placemark.properties.get('iconContent');

//                        // Если открыт поиск маршрутов, можем выбрать эту остановку
//                        const startSelect = document.getElementById('startStopSelect');
//                        const endSelect = document.getElementById('endStopSelect');

//                        if (startSelect && endSelect) {
//                            // Открываем балун с выбором действия
//                            placemark.properties.set('balloonContentFooter', `
//                                <div class="mt-2">
//                                    <button class="btn btn-primary btn-sm" id="selectAsStart">Выбрать как начало</button>
//                                    <button class="btn btn-success btn-sm" id="selectAsEnd">Выбрать как конец</button>
//                                </div>
//                            `);

//                            // После открытия балуна добавляем обработчики на кнопки
//                            placemark.events.add('balloonopen', function () {
//                                setTimeout(() => {
//                                    document.getElementById('selectAsStart')?.addEventListener('click', function () {
//                                        startSelect.value = stopId;
//                                        // Вызываем событие change для обработки выбора
//                                        startSelect.dispatchEvent(new Event('change'));
//                                        placemark.balloon.close();
//                                    });

//                                    document.getElementById('selectAsEnd')?.addEventListener('click', function () {
//                                        endSelect.value = stopId;
//                                        // Вызываем событие change для обработки выбора
//                                        endSelect.dispatchEvent(new Event('change'));
//                                        placemark.balloon.close();
//                                    });
//                                }, 100);
//                            });
//                        }
//                    });

//                    stopsCollection.add(placemark);
//                }
//            });

//            // Добавляем коллекцию на карту
//            myMap.geoObjects.add(stopsCollection);

//            // Масштабируем карту, чтобы были видны все метки
//            if (stopsCollection.getLength() > 0) {
//                myMap.setBounds(stopsCollection.getBounds(), {
//                    checkZoomRange: true,
//                    zoomMargin: 30
//                });
//            }
//        }

//        // Функция заполнения выпадающих списков с остановками
//        function populateStopsDropdowns(stops) {
//            const startSelect = document.getElementById('startStopSelect');
//            const endSelect = document.getElementById('endStopSelect');

//            if (startSelect && endSelect) {
//                // Очищаем текущие опции
//                startSelect.innerHTML = '<option value="">Выберите точку отправления</option>';
//                endSelect.innerHTML = '<option value="">Выберите точку прибытия</option>';

//                // Добавляем остановки в выпадающие списки
//                stops.forEach(stop => {
//                    const option = document.createElement('option');
//                    option.value = stop.stopId;
//                    option.textContent = stop.name;

//                    const optionCopy = option.cloneNode(true);

//                    startSelect.appendChild(option);
//                    endSelect.appendChild(optionCopy);
//                });
//            }
//        }

//        // Функция инициализации поиска
//        function initSearch() {
//            // Поиск по названию маршрута
//            const routeNameInput = document.getElementById('routeNameSearch');
//            // Поиск по остановкам
//            const startStopSelect = document.getElementById('startStopSelect');
//            const endStopSelect = document.getElementById('endStopSelect');

//            // Обработчик формы поиска
//            searchForm.addEventListener('submit', async function (e) {
//                e.preventDefault();

//                // Собираем данные поиска
//                const searchData = {
//                    routeName: routeNameInput?.value || '',
//                    startStopId: startStopSelect?.value ? parseInt(startStopSelect.value) : null,
//                    endStopId: endStopSelect?.value ? parseInt(endStopSelect.value) : null
//                };

//                console.log("Поиск маршрутов с параметрами:", searchData);

//                // Выполняем поиск
//                await searchRoutes(searchData);
//            });

//            // Обработчики изменения выбора остановок
//            if (startStopSelect && endStopSelect) {
//                startStopSelect.addEventListener('change', highlightSelectedStop);
//                endStopSelect.addEventListener('change', highlightSelectedStop);
//            }
//        }

//        // Функция выделения выбранных остановок на карте
//        function highlightSelectedStop() {
//            const startStopId = document.getElementById('startStopSelect').value;
//            const endStopId = document.getElementById('endStopSelect').value;

//            // Сбрасываем все метки к стандартному виду
//            stopsCollection.each(function (placemark) {
//                placemark.options.set('preset', 'islands#blueStretchyIcon');
//            });

//            // Подсвечиваем выбранные остановки
//            if (startStopId) {
//                stopsCollection.each(function (placemark) {
//                    if (placemark.properties.get('stopId') === parseInt(startStopId)) {
//                        placemark.options.set('preset', 'islands#greenStretchyIcon');
//                    }
//                });
//            }

//            if (endStopId) {
//                stopsCollection.each(function (placemark) {
//                    if (placemark.properties.get('stopId') === parseInt(endStopId)) {
//                        placemark.options.set('preset', 'islands#redStretchyIcon');
//                    }
//                });
//            }
//        }

//        // Функция поиска маршрутов
//        async function searchRoutes(searchData) {
//            try {
//                // Показываем индикатор загрузки
//                const resultsContainer = document.getElementById('searchResults');
//                if (resultsContainer) {
//                    resultsContainer.innerHTML = '<div class="alert alert-info">Поиск маршрутов...</div>';
//                }

//                // Выполняем поиск маршрутов
//                let apiUrl = '/Route/SearchRoutes';

//                // Если указаны начальная и конечная остановки, используем FindRoutesByStops
//                if (searchData.startStopId && searchData.endStopId) {
//                    const response = await fetch('/Route/FindRoutesByStops', {
//                        method: 'POST',
//                        headers: {
//                            'Content-Type': 'application/json'
//                        },
//                        body: JSON.stringify({
//                            startStopIds: [searchData.startStopId],
//                            endStopIds: [searchData.endStopId]
//                        })
//                    });

//                    if (!response.ok) {
//                        throw new Error(`Ошибка HTTP: ${response.status}`);
//                    }

//                    const data = await response.json();
//                    displaySearchResults(data.routes || []);
//                }
//                // Если указано только название, ищем по нему
//                else if (searchData.routeName.trim() !== '') {
//                    const response = await fetch(`${apiUrl}?name=${encodeURIComponent(searchData.routeName)}`);

//                    if (!response.ok) {
//                        throw new Error(`Ошибка HTTP: ${response.status}`);
//                    }

//                    const data = await response.json();
//                    displaySearchResults(data.routes || []);
//                }
//                // Если ничего не указано
//                else {
//                    if (resultsContainer) {
//                        resultsContainer.innerHTML = '<div class="alert alert-warning">Пожалуйста, укажите параметры поиска</div>';
//                    }
//                }
//            } catch (error) {
//                console.error("Ошибка при поиске маршрутов:", error);
//                const resultsContainer = document.getElementById('searchResults');
//                if (resultsContainer) {
//                    resultsContainer.innerHTML = `<div class="alert alert-danger">Ошибка: ${error.message}</div>`;
//                }
//            }
//        }

//        // Функция отображения результатов поиска
//        function displaySearchResults(routes) {
//            const resultsContainer = document.getElementById('searchResults');
//            if (!resultsContainer) return;

//            if (routes.length === 0) {
//                resultsContainer.innerHTML = '<div class="alert alert-warning">Маршруты не найдены</div>';
//                return;
//            }

//            // Формируем HTML для результатов
//            let html = '<div class="row">';

//            routes.forEach(route => {
//                html += `
//                    <div class="col-md-6 mb-4">
//                        <div class="card h-100">
//                            <div class="card-header d-flex justify-content-between">
//                                <h5 class="mb-0">${route.Name || route.name}</h5>
//                                <span class="badge bg-primary">${route.TransporterName || route.transporterName || ''}</span>
//                            </div>
//                            <div class="card-body">
//                                <p>${route.Description || route.description || 'Нет описания'}</p>
//                                <p><strong>Расписание:</strong> ${route.Schedule || route.schedule || 'Не указано'}</p>
//                                <div class="d-flex mt-3">
//                                    <button class="btn btn-outline-primary me-2 show-on-map" data-route-id="${route.RouteId || route.routeId}">
//                                        Показать на карте
//                                    </button>
//                                    <a href="/Route/RouteDetails?routeID=${route.RouteId || route.routeId}" class="btn btn-primary">
//                                        Подробнее
//                                    </a>
//                                </div>
//                            </div>
//                        </div>
//                    </div>
//                `;
//            });

//            html += '</div>';
//            resultsContainer.innerHTML = html;

//            // Добавляем обработчики для кнопок "Показать на карте"
//            document.querySelectorAll('.show-on-map').forEach(btn => {
//                btn.addEventListener('click', async function () {
//                    const routeId = this.getAttribute('data-route-id');
//                    await showRouteOnMap(routeId);
//                });
//            });
//        }

//        // Функция отображения маршрута на карте
//        async function showRouteOnMap(routeId) {
//            try {
//                console.log("Загрузка маршрута для отображения на карте:", routeId);

//                // Загружаем данные маршрута
//                const response = await fetch(`/Route/GetRouteMapData?id=${routeId}`);
//                if (!response.ok) {
//                    throw new Error(`Ошибка HTTP: ${response.status}`);
//                }

//                const data = await response.json();
//                if (!data.success) {
//                    throw new Error(data.error || "Ошибка при загрузке данных маршрута");
//                }

//                // Парсим данные маршрута
//                const mapData = JSON.parse(data.map);

//                // Удаляем все линии маршрутов, но оставляем метки остановок
//                myMap.geoObjects.each(function (obj) {
//                    if (obj instanceof ymaps.Polyline) {
//                        myMap.geoObjects.remove(obj);
//                    }
//                });

//                // Добавляем линию маршрута
//                if (mapData.lines && mapData.lines.length > 0) {
//                    const polyline = new ymaps.Polyline(mapData.lines, {}, {
//                        strokeColor: '#FF0000',
//                        strokeWidth: 4,
//                        strokeOpacity: 0.6
//                    });

//                    myMap.geoObjects.add(polyline);

//                    // Масштабируем карту под маршрут
//                    myMap.setBounds(polyline.geometry.getBounds(), {
//                        checkZoomRange: true,
//                        zoomMargin: 30
//                    });
//                }

//                // Выделяем остановки этого маршрута
//                if (mapData.stops && mapData.stops.length > 0) {
//                    // Сначала сбрасываем все метки к стандартному виду
//                    stopsCollection.each(function (placemark) {
//                        placemark.options.set('preset', 'islands#blueStretchyIcon');
//                    });

//                    // Затем выделяем остановки маршрута
//                    const routeStopIds = mapData.stops.map(stop => stop.StopId || stop.stopId);

//                    stopsCollection.each(function (placemark) {
//                        const stopId = placemark.properties.get('stopId');
//                        if (routeStopIds.includes(stopId)) {
//                            placemark.options.set('preset', 'islands#violetStretchyIcon');
//                        }
//                    });

//                    // Особенно выделяем первую и последнюю остановки
//                    const firstStopId = routeStopIds[0];
//                    const lastStopId = routeStopIds[routeStopIds.length - 1];

//                    stopsCollection.each(function (placemark) {
//                        const stopId = placemark.properties.get('stopId');
//                        if (stopId === firstStopId) {
//                            placemark.options.set('preset', 'islands#greenStretchyIcon');
//                        } else if (stopId === lastStopId) {
//                            placemark.options.set('preset', 'islands#redStretchyIcon');
//                        }
//                    });
//                }
//            } catch (error) {
//                console.error("Ошибка при отображении маршрута:", error);
//                alert("Не удалось отобразить маршрут на карте");
//            }
//        }
//    });
//});