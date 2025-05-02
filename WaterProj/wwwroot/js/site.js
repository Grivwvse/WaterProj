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

    document.addEventListener('DOMContentLoaded', function () {
        const profileImageInput = document.querySelector('input[name="profileImage"]');
        if (profileImageInput) {
            profileImageInput.addEventListener('change', function (event) {
                const file = event.target.files[0];
                if (file) {
                    const reader = new FileReader();
                    reader.onload = function (e) {
                        const img = document.querySelector('.rounded-circle');
                        if (img) {
                            img.src = e.target.result;
                        }
                    };
                    reader.readAsDataURL(file);
                }
            });
        }
    });
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
    // Массив для хранения существующих остановок из базы данных
    let existingStops = [];
    // Коллекция для хранения меток существующих остановок
    let existingStopsCollection;

    function init() {
        const myMap = new ymaps.Map('myMap', {
            center: [59.938339, 30.313558],
            zoom: 10,
            controls: ['zoomControl', 'typeSelector']
        });

        // Коллекция для маршрутных остановок (выбранные пользователем)
        const placemarkCollection = new ymaps.GeoObjectCollection();
        // Коллекция для отображения существующих остановок из БД
        existingStopsCollection = new ymaps.GeoObjectCollection();

        // Важно: добавляем коллекцию маршрутных остановок на карту сразу
        myMap.geoObjects.add(placemarkCollection);

        let routePolyline = null;

        // Загружаем и отображаем существующие остановки
        loadExistingStops();

        // Функция загрузки существующих остановок
        async function loadExistingStops() {
            try {
                const response = await fetch('/Route/GetAllStops');
                if (!response.ok) {
                    throw new Error(`Ошибка HTTP: ${response.status}`);
                }
                const data = await response.json();
                if (data && data.success) {
                    existingStops = data.stops || [];
                    console.log(`Загружено ${existingStops.length} существующих остановок`);

                    // Отображаем существующие остановки на карте
                    showExistingStopsOnMap();
                }
            } catch (error) {
                console.error('Ошибка при загрузке существующих остановок:', error);
            }
        }

        // Функция отображения существующих остановок на карте
        function showExistingStopsOnMap() {
            // Очищаем текущие метки
            existingStopsCollection.removeAll();

            existingStops.forEach(stop => {
                if (stop.latitude !== undefined && stop.longitude !== undefined) {
                    const placemark = new ymaps.Placemark([stop.latitude, stop.longitude], {
                        iconContent: stop.name,
                        hintContent: stop.name,
                        balloonContentHeader: 'Существующая остановка',
                        balloonContent: `ID: ${stop.stopId}, Координаты: ${stop.latitude.toFixed(6)}, ${stop.longitude.toFixed(6)}`,
                        stopId: stop.stopId
                    }, {
                        preset: 'islands#grayStretchyIcon', // Серый цвет для существующих остановок
                        draggable: false
                    });

                    // Обработчик клика по существующей остановке
                    placemark.events.add('click', function () {
                        // Показываем информационное окно с кнопкой добавления
                        placemark.properties.set('balloonContentFooter', `
                            <div class="mt-2">
                                <button class="btn btn-success btn-sm" id="addExistingStop">Добавить в маршрут</button>
                            </div>
                        `);

                        // После открытия балуна добавляем обработчик на кнопку
                        placemark.events.add('balloonopen', function () {
                            setTimeout(() => {
                                document.getElementById('addExistingStop')?.addEventListener('click', function () {
                                    // Добавляем существующую остановку в маршрут
                                    addExistingStopToRoute(stop);
                                    placemark.balloon.close();
                                });
                            }, 100);
                        });
                    });

                    existingStopsCollection.add(placemark);
                }
            });

            // Добавляем коллекцию на карту
            myMap.geoObjects.add(existingStopsCollection);

            // Добавляем переключатель видимости существующих остановок
            const mapContainer = document.getElementById('myMap');
            const toggleButton = document.createElement('button');
            toggleButton.className = 'btn btn-sm btn-outline-secondary position-absolute m-2';
            toggleButton.style.zIndex = 100;
            toggleButton.style.right = '10px';
            toggleButton.style.top = '10px';
            toggleButton.textContent = 'Скрыть существующие остановки';
            toggleButton.addEventListener('click', function () {
                const isVisible = existingStopsCollection.isShown();
                if (isVisible) {
                    existingStopsCollection.hide();
                    this.textContent = 'Показать существующие остановки';
                    this.classList.add('btn-secondary');
                    this.classList.remove('btn-outline-secondary');
                } else {
                    existingStopsCollection.show();
                    this.textContent = 'Скрыть существующие остановки';
                    this.classList.remove('btn-secondary');
                    this.classList.add('btn-outline-secondary');
                }
            });

            mapContainer.parentNode.style.position = 'relative';
            mapContainer.parentNode.appendChild(toggleButton);
        }

        // Функция добавления существующей остановки в маршрут
        function addExistingStopToRoute(stop) {
            try {
                // Создаем метку с данными существующей остановки
                const placemark = new ymaps.Placemark([stop.latitude, stop.longitude], {
                    iconContent: stop.name,
                    //hintContent: stop.name,
                    //balloonContentHeader: 'Остановка маршрута',
                    balloonContent: `Координаты: ${stop.latitude.toFixed(6)}, ${stop.longitude.toFixed(6)}<br>
                <small class="text-muted">Использована существующая остановка "${stop.name}"</small>`,
                    existingStopId: stop.stopId // Сохраняем ID существующей остановки
                }, {
                    preset: 'islands#greenStretchyIcon', // Зеленый цвет для добавленных в маршрут остановок
                    draggable: false // Существующие остановки нельзя перемещать
                });

                // Добавляем в коллекцию и на карту
                placemarkCollection.add(placemark);

                // Выбираем добавленную метку
                selectedPlacemark = placemark;
                document.getElementById('coords').value = placemark.geometry.getCoordinates().join(', ');
                //document.getElementById('hint').value = placemark.properties.get('hintContent');
                //document.getElementById('balloon').value = placemark.properties.get('balloonContent');
                document.getElementById('iconContent').value = placemark.properties.get('iconContent') || '';

                // Показываем уведомление
                const notificationHTML = `<div class="alert alert-success position-fixed top-0 start-50 translate-middle-x mt-3" style="z-index: 9999;">
            Остановка "${stop.name}" добавлена в маршрут
        </div>`;

                const notificationElement = document.createElement('div');
                notificationElement.innerHTML = notificationHTML;
                document.body.appendChild(notificationElement.firstChild);

                // Удаляем уведомление через 3 секунды
                setTimeout(() => {
                    const alerts = document.getElementsByClassName('alert');
                    if (alerts.length > 0) alerts[0].remove();
                }, 3000);

                // Получаем обновленные координаты всех меток
                const marksCoords = getMarksCoords(placemarkCollection);
                console.log("Координаты меток после добавления:", marksCoords);

                // Обновляем полилинию маршрута
                if (marksCoords.length >= 2) {
                    if (!routePolyline) {
                        routePolyline = createRoutePolyline(marksCoords);
                    } else {
                        updateRouteGeometry(marksCoords, routePolyline.geometry.getCoordinates());
                    }

                    if (routePolyline) {
                        mapData.lines = routePolyline.geometry.getCoordinates();
                    }
                }

                // Сохраняем данные остановки в массиве
                const coords = placemark.geometry.getCoordinates();
                const stop_data = {
                    Name: placemark.properties.get('iconContent'),
                    Latitude: coords[0],
                    Longitude: coords[1],
                    //Hint: placemark.properties.get('hintContent'),
                    //Balloon: placemark.properties.get('balloonContent'),
                    ExistingStopId: placemark.properties.get('existingStopId')
                };

                stopsData.push(stop_data);
                console.log("Добавлена существующая остановка:", stop_data);

                // Обновляем mapData.stops
                mapData.stops = stopsData;
            } catch (e) {
                console.error("Ошибка при добавлении существующей остановки:", e);
                alert("Произошла ошибка при добавлении остановки: " + e.message);
            }
        }

        // Функция обновления маршрута после добавления остановки
        function updateRouteAfterAddingStop(placemark) {
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
            }

            // Сохраняем данные остановки в массиве
            const coords = placemark.geometry.getCoordinates();
            const stop = {
                Name: placemark.properties.get('iconContent'),
                Latitude: coords[0],
                Longitude: coords[1],
                //Hint: placemark.properties.get('hintContent'),
                //Balloon: placemark.properties.get('balloonContent'),
                ExistingStopId: placemark.properties.get('existingStopId')
            };

            stopsData.push(stop);
            console.log("Добавлена существующая остановка:", stop);

            // Обновляем mapData.stops
            mapData.stops = stopsData;
        }

        function getMarksCoords(collection) {
            const coordsArray = [];
            collection.each(obj => coordsArray.push(obj.geometry.getCoordinates()));
            return coordsArray;
        }

        // Функция для создания и добавления полилинии
        function createRoutePolyline(coords) {
            // Проверка на наличие хотя бы двух точек для построения линии
            if (!coords || coords.length < 2) {
                console.warn("Недостаточно точек для построения полилинии");
                return null;
            }

            console.log("Создание полилинии с координатами:", coords);

            // Удаляем предыдущую линию, если она существует
            if (routePolyline) {
                // Сначала останавливаем редактирование, чтобы избежать ошибок
                try {
                    if (routePolyline.editor && routePolyline.editor.state && routePolyline.editor.state.get('editing')) {
                        routePolyline.editor.stopEditing();
                    }
                } catch (e) {
                    console.error("Ошибка при остановке редактирования:", e);
                }

                // Теперь удаляем полилинию
                myMap.geoObjects.remove(routePolyline);
                routePolyline = null;
            }

            // Создаем новую полилинию
            try {
                routePolyline = new ymaps.Polyline(coords, {}, {
                    strokeColor: '#FF0000',
                    strokeWidth: 4,
                    strokeOpacity: 0.6,
                    editorDrawingCursor: "crosshair",
                    editorMaxPoints: 100,
                    draggable: false
                });

                myMap.geoObjects.add(routePolyline);

                // Добавляем редактор полилинии
                if (routePolyline.editor) {
                    routePolyline.editor.startEditing();

                    // Обработчик события изменения геометрии полилинии
                    routePolyline.geometry.events.add('change', function () {
                        // Получаем все координаты полилинии, включая промежуточные точки
                        const lineCoordinates = routePolyline.geometry.getCoordinates();
                        mapData.lines = lineCoordinates;
                        console.log("Полилиния обновлена, точек:", lineCoordinates.length);
                    });
                } else {
                    console.warn("Редактор полилинии недоступен");
                }

                return routePolyline;
            } catch (e) {
                console.error("Ошибка при создании полилинии:", e);
                return null;
            }
        }

        // Изменяем обработчик клика по карте для создания только новых остановок
        // БЕЗ автоматического определения ближайших существующих остановок
        // Изменяем обработчик клика по карте для создания только новых остановок
        // Заменить эту часть в обработчике клика по карте
        myMap.events.add('click', function (e) {
            try {
                const coords = e.get('coords');
                console.log("Клик по карте в координатах:", coords);

                // Заполняем поля в модальном окне
                document.getElementById('modal-stopCoords').textContent = `${coords[0].toFixed(6)}, ${coords[1].toFixed(6)}`;
                document.getElementById('modal-stopName').value = 'Новая остановка';
                //document.getElementById('modal-stopHint').value = 'Новая остановка';
                //document.getElementById('modal-stopDescription').value = '';

                // Временно сохраняем координаты для использования при сохранении
                window.tempStopCoords = coords;

                // Показываем модальное окно
                const modal = new bootstrap.Modal(document.getElementById('createStopModal'));
                modal.show();

            } catch (e) {
                console.error("Ошибка при обработке клика по карте:", e);
                alert("Произошла ошибка при создании новой остановки: " + e.message);
            }
        });

        // Добавляем обработчик кнопки "Сохранить" в модальном окне
        document.getElementById('saveStopFromModal').addEventListener('click', function () {
            try {
                // Получаем данные из формы
                const stopName = document.getElementById('modal-stopName').value;
                //const stopHint = document.getElementById('modal-stopHint').value;
                //const stopDescription = document.getElementById('modal-stopDescription').value;
                const coords = window.tempStopCoords;

                if (!stopName || !coords) {
                    alert("Пожалуйста, заполните название остановки");
                    return;
                }

                // Создаем новую остановку с данными из модального окна
                const placemark = new ymaps.Placemark(coords, {
                    iconContent: stopName,
                    balloonContentHeader: stopName,
                    balloonContentBody: "",
                    hintContent: "",
                    balloonContent: `Координаты: ${coords[0].toFixed(6)}, ${coords[1].toFixed(6)}<br>`,
                    existingStopId: null // Новая остановка, нет ID
                }, {
                    preset: 'islands#blueStretchyIcon', // Синий цвет для новых остановок
                    draggable: true // Новые остановки можно перемещать
                });

                // Добавляем метку в коллекцию
                placemarkCollection.add(placemark);
                console.log(`В коллекции остановок сейчас ${placemarkCollection.getLength()} меток`);

                // Назначаем обработчик события клика на метку
                placemark.events.add('click', () => {
                    selectedPlacemark = placemark;
                    document.getElementById('coords').value = placemark.geometry.getCoordinates().join(', ');
                   // document.getElementById('hint').value = placemark.properties.get('hintContent');
                    //document.getElementById('balloon').value = placemark.properties.get('balloonContent');
                    document.getElementById('iconContent').value = placemark.properties.get('iconContent') || '';
                });

                // После добавления метки на карту, сохраняем данные о новой остановке в массив stopsData
                const newStop = {
                    Name: stopName,
                    Latitude: coords[0],
                    Longitude: coords[1],
                    Hint: "",
                    Balloon: "",
                    ExistingStopId: null // Новая остановка, нет ID
                };

                stopsData.push(newStop);
                console.log("Новая остановка добавлена:", newStop);

                // Обновляем массив остановок в mapData
                mapData.stops = stopsData;

                // Обновляем маршрут с новой остановкой
                updateRouteAfterAddingStop(placemark);

                // Добавляем визуальное уведомление
                const notificationHTML = `<div class="alert alert-success position-fixed top-0 start-50 translate-middle-x mt-3" style="z-index: 9999;">
            Остановка "${stopName}" добавлена в маршрут
        </div>`;

                const notificationElement = document.createElement('div');
                notificationElement.innerHTML = notificationHTML;
                document.body.appendChild(notificationElement.firstChild);

                // Удаляем уведомление через 3 секунды
                setTimeout(() => {
                    const alerts = document.getElementsByClassName('alert');
                    if (alerts.length > 0) alerts[0].remove();
                }, 3000);

                // Делаем эту остановку выбранной для редактирования
                selectedPlacemark = placemark;
                document.getElementById('coords').value = coords.join(', ');
                //document.getElementById('hint').value = stopHint;
                //document.getElementById('balloon').value = stopDescription;
                document.getElementById('iconContent').value = stopName;

                // Закрываем модальное окно
                const modalElement = document.getElementById('createStopModal');
                const modalInstance = bootstrap.Modal.getInstance(modalElement);
                modalInstance.hide();

            } catch (e) {
                console.error("Ошибка при сохранении остановки:", e);
                alert("Произошла ошибка при сохранении остановки: " + e.message);
            }
        });

        // Функция для обновления геометрии маршрута
        function updateRouteGeometry(marksCoords, lineCoordinates) {
            // Проверяем наличие координат
            if (!marksCoords || marksCoords.length === 0) {
                console.warn("Нет координат меток для обновления геометрии");
                return;
            }

            // Если у нас меньше двух точек, нельзя построить линию
            if (marksCoords.length < 2) {
                console.warn("Для построения линии нужно минимум две точки");
                return;
            }

            console.log("Обновление геометрии маршрута:",
                "Меток:", marksCoords.length,
                "Точек линии:", lineCoordinates ? lineCoordinates.length : 0);

            try {
                // Удаляем старую полилинию безопасно
                if (routePolyline) {
                    // Сначала останавливаем редактирование, чтобы избежать ошибок
                    try {
                        if (routePolyline.editor && routePolyline.editor.state &&
                            routePolyline.editor.state.get('editing')) {
                            routePolyline.editor.stopEditing();
                        }
                    } catch (e) {
                        console.error("Ошибка при остановке редактирования:", e);
                    }

                    myMap.geoObjects.remove(routePolyline);
                    routePolyline = null;
                }

                // Если это первая линия или нет данных о предыдущей геометрии
                if (!lineCoordinates || lineCoordinates.length < 2) {
                    console.log("Создание новой полилинии из меток");
                    routePolyline = createRoutePolyline(marksCoords);
                    return;
                }

                // Объединяем координаты с учетом формы линии
                let enhancedCoords;
                try {
                    enhancedCoords = preserveLineShapeWithStops(marksCoords, lineCoordinates);
                } catch (e) {
                    console.error("Ошибка при объединении координат:", e);
                    enhancedCoords = marksCoords; // Используем просто метки, если произошла ошибка
                }

                // Создаем новую полилинию
                routePolyline = createRoutePolyline(enhancedCoords);

                console.log("Маршрут обновлён, точек в линии:",
                    routePolyline ? routePolyline.geometry.getCoordinates().length : 0);
            } catch (e) {
                console.error("Ошибка при обновлении геометрии маршрута:", e);
            }
        }

        // Функция для объединения координат меток с существующими координатами линии
        function preserveLineShapeWithStops(marksCoords, lineCoordinates) {
            // Если линии нет или она содержит всего 2 точки, просто возвращаем координаты остановок
            if (!lineCoordinates || lineCoordinates.length <= 2) {
                return marksCoords;
            }

            // Если у нас 2 остановки или меньше, используем всю линию
            if (marksCoords.length <= 2) {
                return lineCoordinates;
            }

            console.log("Обработка сложного маршрута с", marksCoords.length, "остановками");

            // Для маршрутов с 3+ остановками используем простую стратегию:
            // Просто соединяем остановки прямыми линиями
            if (marksCoords.length >= 3) {
                console.log("Применяем упрощенную обработку для маршрута с 3+ остановками");
                return marksCoords;
            }

            // Для маршрутов с 2 остановками сохраняем старую логику
            const result = [marksCoords[0]];

            // Для каждой пары последовательных маркеров
            for (let i = 0; i < marksCoords.length - 1; i++) {
                const currentMark = marksCoords[i];
                const nextMark = marksCoords[i + 1];

                // Находим ближайшие точки в существующей линии к текущему и следующему маркеру
                const currentIndex = findNearestPointIndex(lineCoordinates, currentMark);
                const nextIndex = findNearestPointIndex(lineCoordinates, nextMark);

                // Устанавливаем лимит на количество промежуточных точек для предотвращения зависания
                const maxIntermediatePoints = 50;

                // Если оба индекса найдены
                if (currentIndex !== -1 && nextIndex !== -1) {
                    // Определяем направление движения по линии
                    const step = currentIndex < nextIndex ? 1 : -1;

                    // Рассчитываем количество точек между текущим и следующим маркером
                    let pointCount = Math.abs(nextIndex - currentIndex);

                    // Ограничиваем количество точек, если их слишком много
                    if (pointCount > maxIntermediatePoints) {
                        console.log(`Ограничиваем количество промежуточных точек с ${pointCount} до ${maxIntermediatePoints}`);
                        pointCount = maxIntermediatePoints;
                    }

                    // Шаг для выборки точек (чтобы не брать все подряд, если их много)
                    const skipStep = Math.max(1, Math.floor(Math.abs(nextIndex - currentIndex) / pointCount));

                    // Добавляем промежуточные точки, если они есть, с шагом skipStep
                    let pointsAdded = 0;
                    for (let j = currentIndex + step; j !== nextIndex && pointsAdded < maxIntermediatePoints; j += step * skipStep) {
                        if (j >= 0 && j < lineCoordinates.length) {
                            result.push(lineCoordinates[j]);
                            pointsAdded++;
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

        // Обработка кнопки "Сохранить остановку" для новых остановок
        document.getElementById('saveStop').addEventListener('click', () => {
            if (!selectedPlacemark) {
                alert("Сначала выберите метку!");
                return;
            }

            const currentIconContent = selectedPlacemark.properties.get("iconContent");
            const existingStopId = selectedPlacemark.properties.get("existingStopId");

            // Если это существующая остановка, сообщаем что её нельзя изменить
            if (existingStopId) {
                alert("Это существующая остановка. Её параметры не могут быть изменены.");
                return;
            }

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

            // Удаляем старую метку из коллекции
            placemarkCollection.remove(selectedPlacemark);

            // Создаём новую метку
            const newPlacemark = new ymaps.Placemark(coords, {
                iconContent: iconContent,
                hintContent: hint,
                balloonContentHeader: 'Остановка маршрута',
                balloonContentBody: `${balloon}`,
                balloonContent: `Координаты: ${coords[0].toFixed(6)}, ${coords[1].toFixed(6)}<br>
                ${balloon}`,
                existingStopId: null // Это новая остановка
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
                ExistingStopId: null // Это новая остановка
            };

            stopsData.push(stop);
            console.log("Остановка обновлена:", stop);
            alert("Остановка сохранена. Всего: " + stopsData.length);

            // Обновляем карту с новыми данными
            mapData.stops = stopsData;

            // Обновляем линии из текущей полилинии со всеми промежуточными точками
            if (routePolyline) {
                mapData.lines = routePolyline.geometry.getCoordinates();
            }
        });

        // Сохранение маршрута
        document.getElementById('saveRoute').addEventListener('click', async () => {
            const name = document.getElementById('routeName').value.trim();
            const description = document.getElementById('routeDescription').value.trim();
            const scheduleDescription = document.getElementById('scheduleDescription').value.trim();
            const shipId = document.getElementById('shipSelect').value;

            if (!name || stopsData.length === 0) {
                alert("Введите название маршрута и добавьте хотя бы одну остановку!");
                return;
            }

            if (!shipId) {
                alert("Выберите судно для маршрута!");
                return;
            }

            // Завершаем редактирование полилинии перед сохранением
            if (routePolyline && routePolyline.editor.state.get('editing')) {
                routePolyline.editor.stopEditing();

                // Обновляем данные линий в mapData
                mapData.lines = routePolyline.geometry.getCoordinates();
            }

            // Логирование для отладки
            console.log("Отправляемые данные остановок:", stopsData);
            console.log("Из них существующих остановок:", stopsData.filter(s => s.ExistingStopId).length);

            const routeData = {
                Name: name,
                Description: description,
                Schedule: scheduleDescription,
                ShipId: parseInt(shipId),
                Stops: stopsData, // Содержит ExistingStopId для существующих остановок
                Polyline: mapData.lines,
                Map: JSON.stringify(mapData)
            };

            // Выводим содержимое объекта для отладки
            console.log('Отправляемые данные на сервер:', JSON.stringify(routeData, null, 2));

            // Создаем FormData для отправки маршрута и файлов
            const formData = new FormData();
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
                    // window.location.href = `/Route/RouteDetails?routeID=${result.routeId}`;
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


// Добавляем код для отображения карты на странице деталей маршрута
document.addEventListener("DOMContentLoaded", function () {
    // Проверяем, находимся ли мы на странице деталей маршрута
    if (!window.location.pathname.includes("/Route/RouteDetails")) return;

    // Проверяем, есть ли элемент карты на странице
    const routeMapContainer = document.getElementById('routeMap');
    if (!routeMapContainer) {
        console.error("Контейнер для карты маршрута не найден на странице");
        return;
    }

    console.log("Инициализация карты для страницы деталей маршрута");

    // Получаем ID маршрута из URL
    const urlParams = new URLSearchParams(window.location.search);
    const routeId = urlParams.get('routeID');

    if (!routeId) {
        console.error("ID маршрута не найден в URL");
        return;
    }

    // Инициализируем карту при загрузке API Яндекс.Карт
    ymaps.ready(function () {
        // Создаем карту
        const myMap = new ymaps.Map('routeMap', {
            center: [59.938339, 30.313558], // Начальный центр - Санкт-Петербург
            zoom: 10,
            controls: ['zoomControl', 'typeSelector']
        });

        console.log("Карта создана, загружаем данные маршрута ID:", routeId);

        // Загружаем данные маршрута и отображаем на карте
        loadRouteAndDisplay(routeId, myMap);
    });

    // Функция загрузки данных маршрута и отображения на карте
    async function loadRouteAndDisplay(routeId, map) {
        try {
            // Загружаем данные маршрута
            const response = await fetch(`/Route/GetRouteMapData?id=${routeId}`);
            if (!response.ok) {
                throw new Error(`Ошибка HTTP: ${response.status}`);
            }

            const data = await response.json();
            if (!data.success) {
                throw new Error(data.error || "Ошибка при загрузке данных маршрута");
            }

            console.log("Данные маршрута получены:", data);

            // Парсим данные маршрута
            const mapData = JSON.parse(data.map);

            // Создаем коллекцию для остановок
            const stopsCollection = new ymaps.GeoObjectCollection();

            // Добавляем остановки на карту
            if (mapData.stops && mapData.stops.length > 0) {
                mapData.stops.forEach((stop, index) => {
                    if (stop.Latitude && stop.Longitude) {
                        const isFirstStop = index === 0;
                        const isLastStop = index === mapData.stops.length - 1;

                        // Выбираем цвет метки в зависимости от положения остановки в маршруте
                        let preset = 'islands#blueStretchyIcon';
                        if (isFirstStop) {
                            preset = 'islands#greenStretchyIcon'; // Первая остановка - зеленая
                        } else if (isLastStop) {
                            preset = 'islands#redStretchyIcon'; // Последняя остановка - красная
                        }

                        const placemark = new ymaps.Placemark(
                            [stop.Latitude, stop.Longitude],
                            {
                                iconContent: stop.Name || `Остановка ${index + 1}`,
                                hintContent: stop.Name || `Остановка ${index + 1}`,
                                balloonContentHeader: 'Остановка маршрута',
                                balloonContent: `Координаты: ${stop.Latitude.toFixed(6)}, ${stop.Longitude.toFixed(6)}`
                            },
                            {
                                preset: preset
                            }
                        );

                        stopsCollection.add(placemark);
                    }
                });

                // Добавляем коллекцию остановок на карту
                map.geoObjects.add(stopsCollection);
            }

            // Добавляем линию маршрута
            if (mapData.lines && mapData.lines.length > 0) {
                const polyline = new ymaps.Polyline(mapData.lines, {}, {
                    strokeColor: '#FF0000',
                    strokeWidth: 4,
                    strokeOpacity: 0.6
                });

                map.geoObjects.add(polyline);

                // Масштабируем карту под маршрут
                map.setBounds(polyline.geometry.getBounds(), {
                    checkZoomRange: true,
                    zoomMargin: 30
                });
            } else if (stopsCollection.getLength() > 0) {
                // Если нет линии, но есть остановки, масштабируем под остановки
                map.setBounds(stopsCollection.getBounds(), {
                    checkZoomRange: true,
                    zoomMargin: 30
                });
            }

            console.log("Маршрут успешно отображен на карте");

        } catch (error) {
            console.error("Ошибка при загрузке и отображении маршрута:", error);
            // Добавляем сообщение об ошибке на карту
            const errorElement = document.createElement('div');
            errorElement.className = 'alert alert-danger';
            errorElement.textContent = 'Ошибка при загрузке маршрута. Пожалуйста, попробуйте позже.';
            routeMapContainer.appendChild(errorElement);
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