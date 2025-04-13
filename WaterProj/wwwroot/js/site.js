// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

if (window.location.pathname.includes("/ConsumerAccount"))
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
