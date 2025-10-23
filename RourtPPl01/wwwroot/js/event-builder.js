// Event Builder JavaScript
let sectionCounter = 0;
let surveyCounter = 0;
let discussionCounter = 0;
let tableCounter = 0;
let decisionIdCounter = 0;
let sectionQuestionIdCounter = 0;

// Add Section
function addSection() {
    const container = document.getElementById('sectionsContainer');
    const first = container.firstElementChild;
    if (first && first.tagName === 'P' && first.classList.contains('text-muted')) {
        first.remove();
    }

    const sectionHtml = `
        <div class="card mb-3 section-item" data-section-id="${sectionCounter}">
            <div class="card-header bg-light d-flex justify-content-between align-items-center">
                <span><i class="fas fa-grip-vertical me-2"></i>بند ${sectionCounter + 1}</span>
                <div class="d-flex gap-2">
                    <div class="dropdown">
                        <button type="button" class="btn btn-sm btn-success dropdown-toggle" data-bs-toggle="dropdown">
                            <i class="fas fa-plus me-1"></i>إضافة مكوّن للبند
                        </button>
                        <ul class="dropdown-menu">
                            <li><a class="dropdown-item" href="#" onclick="addSurveyToSection(${sectionCounter});return false;"><i class="fas fa-poll me-2"></i>استبيان</a></li>
                            <li><a class="dropdown-item" href="#" onclick="addDiscussionToSection(${sectionCounter});return false;"><i class="fas fa-comments me-2"></i>نقاش</a></li>
                            <li><a class="dropdown-item" href="#" onclick="addTableToSection(${sectionCounter});return false;"><i class="fas fa-table me-2"></i>جدول</a></li>
                            <li><hr class="dropdown-divider"></li>
                            <li><a class="dropdown-item" href="#" onclick="document.getElementById('sec-imageUpload-${sectionCounter}').click();return false;"><i class="fas fa-image me-2"></i>صورة</a></li>
                            <li><a class="dropdown-item" href="#" onclick="document.getElementById('sec-pdfUpload-${sectionCounter}').click();return false;"><i class="fas fa-file-pdf me-2"></i>PDF</a></li>
                        </ul>
                    </div>
                    <button type="button" class="btn btn-sm btn-outline-primary" onclick="addDecision(${sectionCounter})">
                        <i class="fas fa-plus me-1"></i>إضافة قرار
                    </button>
                    <button type="button" class="btn btn-sm btn-outline-danger" onclick="removeSection(${sectionCounter})">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            </div>
            <div class="card-body">
                <div class="mb-3">
                    <label class="form-label">عنوان البند</label>
                    <input type="text" name="Sections[${sectionCounter}].Title" class="form-control section-title" required>
                </div>
                <div class="mb-3">
                    <label class="form-label">نص البند</label>
                    <textarea name="Sections[${sectionCounter}].Body" class="form-control section-body" rows="3"></textarea>
                </div>
                <input type="hidden" name="Sections[${sectionCounter}].Order" value="${sectionCounter}">

                <div class="decisions-container" id="decisions-${sectionCounter}">
                    <p class="text-muted small">لا توجد قرارات</p>
                </div>
                <hr/>
                <div id="sec-components-${sectionCounter}" class="section-components">
                    <p class="text-muted small mb-0">لا توجد مكونات لهذا البند</p>
                </div>
                <input type="file" id="sec-imageUpload-${sectionCounter}" accept="image/*" multiple style="display:none" onchange="handleSectionImageUpload(${sectionCounter}, this)">
                <input type="file" id="sec-pdfUpload-${sectionCounter}" accept=".pdf" multiple style="display:none" onchange="handleSectionPdfUpload(${sectionCounter}, this)">
            </div>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', sectionHtml);
    sectionCounter++;
}

// Remove Section
function removeSection(id) {
    if (confirm('هل أنت متأكد من حذف هذا البند؟')) {
        document.querySelector(`[data-section-id="${id}"]`).remove();
        checkEmptySections();
    }
}

// Add Decision
function addDecision(sectionId) {
    const container = document.getElementById(`decisions-${sectionId}`);
    if (container.querySelector('.text-muted')) {
        container.innerHTML = '';
    }

    const decisionId = ++decisionIdCounter;
    const decisionHtml = `
        <div class="card mb-2 decision-item" data-decision-id="${decisionId}">
            <div class="card-body">
                <div class="d-flex justify-content-between align-items-start mb-2">
                    <label class="form-label mb-0">عنوان القرار</label>
                    <button type="button" class="btn btn-sm btn-outline-danger" onclick="removeDecision(${decisionId})">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
                <input type="text" name="Sections[${sectionId}].Decisions[${decisionId}].Title" class="form-control mb-2" required>

                <label class="form-label">عناصر القرار</label>
                <div class="decision-items" id="decision-items-${decisionId}">
                    <div class="input-group mb-2">
                        <span class="input-group-text">1</span>
                        <input type="text" name="Sections[${sectionId}].Decisions[${decisionId}].Items[0].Text" class="form-control">
                    </div>
                </div>
                <button type="button" class="btn btn-sm btn-outline-secondary" onclick="addDecisionItem(${sectionId}, ${decisionId})">
                    <i class="fas fa-plus me-1"></i>إضافة عنصر
                </button>
            </div>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', decisionHtml);
}

// Remove Decision
function removeDecision(id) {
    document.querySelector(`[data-decision-id="${id}"]`).remove();
}

// Add Decision Item
function addDecisionItem(sectionId, decisionId) {
    const container = document.getElementById(`decision-items-${decisionId}`);
    const itemCount = container.querySelectorAll('.input-group').length;

    const itemHtml = `
        <div class="input-group mb-2">
            <span class="input-group-text">${itemCount + 1}</span>
            <input type="text" name="Sections[${sectionId}].Decisions[${decisionId}].Items[${itemCount}].Text" class="form-control">
            <button type="button" class="btn btn-outline-danger" onclick="this.parentElement.remove()">
                <i class="fas fa-times"></i>
            </button>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', itemHtml);
}

// Add Survey
function addSurvey() {
    const container = document.getElementById('componentsContainer');
    const first = container.firstElementChild;
    if (first && first.tagName === 'P' && first.classList.contains('text-muted')) {
        first.remove();
    }

    const surveyHtml = `
        <div class="card mb-3 component-item" data-type="survey">
            <div class="card-header bg-info bg-opacity-10 d-flex justify-content-between">
                <span><i class="fas fa-poll me-2"></i>استبيان ${surveyCounter + 1}</span>
                <button type="button" class="btn btn-sm btn-outline-danger" onclick="this.closest('.component-item').remove()">
                    <i class="fas fa-trash"></i>
                </button>
            </div>
            <div class="card-body">
                <div class="mb-3">
                    <label class="form-label">عنوان الاستبيان</label>
                    <input type="text" name="Surveys[${surveyCounter}].Title" class="form-control" required>
                </div>
                <div class="mb-3">
                    <label class="form-label">الوصف</label>
                    <textarea name="Surveys[${surveyCounter}].Description" class="form-control" rows="2"></textarea>
                </div>

                <div class="questions-container" id="questions-${surveyCounter}">
                    <p class="text-muted small">لا توجد أسئلة</p>
                </div>

                <button type="button" class="btn btn-sm btn-primary" onclick="addQuestion(${surveyCounter})">
                    <i class="fas fa-plus me-1"></i>إضافة سؤال
                </button>
            </div>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', surveyHtml);
    surveyCounter++;
}

// Add Question
function addQuestion(surveyId) {
    const container = document.getElementById(`questions-${surveyId}`);
    if (container.querySelector('.text-muted')) {
        container.innerHTML = '';
    }

    const questionId = Date.now();
    const questionHtml = `
        <div class="card mb-2" data-question-id="${questionId}">
            <div class="card-body">
                <div class="row g-2 mb-2">
                    <div class="col-md-8">
                        <input type="text" name="Surveys[${surveyId}].Questions[${questionId}].Text" class="form-control" placeholder="نص السؤال" required>
                    </div>
                    <div class="col-md-3">
                        <select name="Surveys[${surveyId}].Questions[${questionId}].Type" class="form-select">
                            <option value="0">اختيار واحد</option>
                            <option value="1">اختيار متعدد</option>
                        </select>
                    </div>
                    <div class="col-md-1">
                        <button type="button" class="btn btn-outline-danger w-100" onclick="this.closest('[data-question-id]').remove()">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </div>

                <div class="options-container" id="options-${questionId}">
                    <div class="input-group input-group-sm mb-1">
                        <span class="input-group-text">خيار 1</span>
                        <input type="text" name="Surveys[${surveyId}].Questions[${questionId}].Options[0].Text" class="form-control">
                    </div>
                    <div class="input-group input-group-sm mb-1">
                        <span class="input-group-text">خيار 2</span>
                        <input type="text" name="Surveys[${surveyId}].Questions[${questionId}].Options[1].Text" class="form-control">
                    </div>
                </div>

                <button type="button" class="btn btn-sm btn-outline-secondary" onclick="addOption(${surveyId}, ${questionId})">
                    <i class="fas fa-plus me-1"></i>خيار
                </button>
            </div>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', questionHtml);
}

// Add Option
function addOption(surveyId, questionId) {
    const container = document.getElementById(`options-${questionId}`);
    const optionCount = container.querySelectorAll('.input-group').length;

    const optionHtml = `
        <div class="input-group input-group-sm mb-1">
            <span class="input-group-text">خيار ${optionCount + 1}</span>
            <input type="text" name="Surveys[${surveyId}].Questions[${questionId}].Options[${optionCount}].Text" class="form-control">
            <button type="button" class="btn btn-outline-danger btn-sm" onclick="this.parentElement.remove()">
                <i class="fas fa-times"></i>
            </button>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', optionHtml);
}

// Add Discussion
function addDiscussion() {
    const container = document.getElementById('componentsContainer');
    const first = container.firstElementChild;
    if (first && first.tagName === 'P' && first.classList.contains('text-muted')) {
        first.remove();
    }

    const discussionHtml = `
        <div class="card mb-3 component-item" data-type="discussion">
            <div class="card-header bg-success bg-opacity-10 d-flex justify-content-between">
                <span><i class="fas fa-comments me-2"></i>نقاش ${discussionCounter + 1}</span>
                <button type="button" class="btn btn-sm btn-outline-danger" onclick="this.closest('.component-item').remove()">
                    <i class="fas fa-trash"></i>
                </button>
            </div>
            <div class="card-body">
                <div class="mb-3">
                    <label class="form-label">عنوان النقاش</label>
                    <input type="text" name="Discussions[${discussionCounter}].Title" class="form-control" required>
                </div>
                <div class="mb-3">
                    <label class="form-label">الغرض من النقاش</label>
                    <textarea name="Discussions[${discussionCounter}].Purpose" class="form-control" rows="3" required></textarea>
                </div>
            </div>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', discussionHtml);
    discussionCounter++;
}

// Add Table
function addTable() {
    const container = document.getElementById('componentsContainer');
    const first = container.firstElementChild;
    if (first && first.tagName === 'P' && first.classList.contains('text-muted')) {
        first.remove();
    }

    const tableHtml = `
        <div class="card mb-3 component-item" data-type="table">
            <div class="card-header bg-warning bg-opacity-10 d-flex justify-content-between">
                <span><i class="fas fa-table me-2"></i>جدول ${tableCounter + 1}</span>
                <button type="button" class="btn btn-sm btn-outline-danger" onclick="this.closest('.component-item').remove()">
                    <i class="fas fa-trash"></i>
                </button>
            </div>
            <div class="card-body">
                <div class="mb-3">
                    <label class="form-label">عنوان الجدول</label>
                    <input type="text" name="Tables[${tableCounter}].Title" class="form-control" required>
                </div>
                <div class="mb-3">

                    <label class="form-label">عدد الصفوف</label>
                    <input type="number" class="form-control" value="3" min="1" max="20" id="table-rows-${tableCounter}">
                </div>
                <div class="mb-3">
                    <label class="form-label">عدد الأعمدة</label>
                    <input type="number" class="form-control" value="3" min="1" max="10" id="table-cols-${tableCounter}">
                </div>
                <button type="button" class="btn btn-primary" onclick="generateTable(${tableCounter})">
                    <i class="fas fa-table me-1"></i>إنشاء الجدول
                </button>
                <div id="table-preview-${tableCounter}" class="mt-3"></div>
                <input type="hidden" name="Tables[${tableCounter}].RowsJson" id="table-data-${tableCounter}">
            </div>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', tableHtml);
    tableCounter++;
}

// Generate Table
function generateTable(tableId) {
    const rows = parseInt(document.getElementById(`table-rows-${tableId}`).value);
    const cols = parseInt(document.getElementById(`table-cols-${tableId}`).value);
    const preview = document.getElementById(`table-preview-${tableId}`);

    let tableHtml = '<table class="table table-bordered table-sm"><tbody>';
    const tableData = [];

    for (let i = 0; i < rows; i++) {
        tableHtml += '<tr>';
        const rowData = [];
        for (let j = 0; j < cols; j++) {
            tableHtml += `<td contenteditable="true" class="p-2" style="min-width:100px"></td>`;
            rowData.push({ value: '', bold: false, italic: false, align: 'right' });
        }
        tableHtml += '</tr>';
        tableData.push(rowData);
    }

    tableHtml += '</tbody></table>';
    preview.innerHTML = tableHtml;

    document.getElementById(`table-data-${tableId}`).value = JSON.stringify({ rows: tableData });
}

// Handle Image Upload
function handleImageUpload(input) {
    const files = input.files;
    const container = document.getElementById('componentsContainer');

    const first = container.firstElementChild;
    if (first && first.tagName === 'P' && first.classList.contains('text-muted')) {
        first.remove();
    }

    for (let file of files) {
        const reader = new FileReader();
        reader.onload = function(e) {
            const imageHtml = `
                <div class="card mb-3 component-item" data-type="image">
                    <div class="card-header bg-primary bg-opacity-10 d-flex justify-content-between">
                        <span><i class="fas fa-image me-2"></i>${file.name}</span>
                        <button type="button" class="btn btn-sm btn-outline-danger" onclick="this.closest('.component-item').remove()">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                    <div class="card-body text-center">
                        <img src="${e.target.result}" class="img-fluid" style="max-height:200px">
                        <input type="hidden" name="Images[]" value="${e.target.result}">
                    </div>
                </div>
            `;
            container.insertAdjacentHTML('beforeend', imageHtml);
        };
        reader.readAsDataURL(file);
    }

    input.value = '';
}

// Handle PDF Upload
function handlePdfUpload(input) {
    const files = input.files;
    const container = document.getElementById('componentsContainer');

    const first = container.firstElementChild;
    if (first && first.tagName === 'P' && first.classList.contains('text-muted')) {
        first.remove();
    }

    for (let file of files) {
        const reader = new FileReader();
        reader.onload = function(e) {
            const pdfHtml = `
                <div class="card mb-3 component-item" data-type="pdf">
                    <div class="card-header bg-danger bg-opacity-10 d-flex justify-content-between">
                        <span><i class="fas fa-file-pdf me-2"></i>${file.name}</span>
                        <button type="button" class="btn btn-sm btn-outline-danger" onclick="this.closest('.component-item').remove()">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                    <div class="card-body">
                        <p class="mb-0"><i class="fas fa-file-pdf me-2 text-danger"></i>ملف PDF - ${(file.size / 1024).toFixed(2)} KB</p>
                        <input type="hidden" name="PDFs[]" value="${e.target.result}">
                    </div>
                </div>
            `;
            container.insertAdjacentHTML('beforeend', pdfHtml);
        };
        reader.readAsDataURL(file);
    }

    input.value = '';
}

// Handle Custom PDF Upload
function handleCustomPdfUpload(input) {
    const files = input.files;
    const container = document.getElementById('componentsContainer');

    const first = container.firstElementChild;
    if (first && first.tagName === 'P' && first.classList.contains('text-muted')) {
        first.remove();
    }

    for (let file of files) {
        const reader = new FileReader();
        reader.onload = function(e) {
            const pdfHtml = `
                <div class="card mb-3 component-item" data-type="custompdf">
                    <div class="card-header bg-danger bg-opacity-10 d-flex justify-content-between">
                        <span><i class="fas fa-file-pdf me-2"></i>${file.name}</span>
                        <button type="button" class="btn btn-sm btn-outline-danger" onclick="this.closest('.component-item').remove()">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                    <div class="card-body">
                        <p class="mb-0"><i class="fas fa-file-pdf me-2 text-danger"></i>PDF مخصص - ${(file.size / 1024).toFixed(2)} KB</p>
                        <input type="hidden" name="CustomPDFs[]" value="${e.target.result}">
                    </div>
                </div>
            `;
            container.insertAdjacentHTML('beforeend', pdfHtml);
        };
        reader.readAsDataURL(file);
    }

    input.value = '';
}


// ===== Section-level components (Create page builder) =====
function ensureSectionContainer(secId){
    const cont = document.getElementById(`sec-components-${secId}`);
    if (!cont) return null;
    const first = cont.firstElementChild;
    if (first && first.tagName === 'P' && first.classList.contains('text-muted')) first.remove();
    return cont;
}
function addSurveyToSection(secId){
    const cont = ensureSectionContainer(secId); if (!cont) return;
    const idx = cont.querySelectorAll('.component-item[data-type="survey"]').length;
    const qContainerId = `sec-${secId}-questions-${idx}`;
    const html = `
        <div class="card mb-3 component-item" data-type="survey" data-sec-survey-idx="${idx}">
            <div class="card-header bg-info bg-opacity-10 d-flex justify-content-between">
                <span><i class="fas fa-poll me-2"></i>استبيان</span>
                <button type="button" class="btn btn-sm btn-outline-danger" onclick="this.closest('.component-item').remove()"><i class="fas fa-trash"></i></button>
            </div>
            <div class="card-body">
                <div class="mb-2">
                    <label class="form-label">عنوان الاستبيان</label>
                    <input type="text" class="form-control" placeholder="عنوان" required>
                </div>
                <div class="mb-2">
                    <label class="form-label">الوصف</label>
                    <textarea class="form-control" rows="2"></textarea>
                </div>
                <div class="questions-container" id="${qContainerId}">
                    <p class="text-muted small">لا توجد أسئلة</p>
                </div>
                <button type="button" class="btn btn-sm btn-primary" onclick="addSectionQuestion(${secId}, ${idx})">
                    <i class="fas fa-plus me-1"></i>إضافة سؤال
                </button>
            </div>
        </div>`;
    cont.insertAdjacentHTML('beforeend', html);
}
function addDiscussionToSection(secId){
    const cont = ensureSectionContainer(secId); if (!cont) return;
    const idx = cont.querySelectorAll('.component-item[data-type="discussion"]').length;
    const html = `
        <div class="card mb-3 component-item" data-type="discussion">
            <div class="card-header bg-success bg-opacity-10 d-flex justify-content-between">
                <span><i class="fas fa-comments me-2"></i>نقاش</span>
                <button type="button" class="btn btn-sm btn-outline-danger" onclick="this.closest('.component-item').remove()"><i class="fas fa-trash"></i></button>
            </div>
            <div class="card-body">
                <div class="mb-2">
                    <label class="form-label">العنوان</label>
                    <input type="text" class="form-control" name="Sections[${secId}].Discussions[${idx}].Title" required>
                </div>
                <div class="mb-2">
                    <label class="form-label">الغرض</label>
                    <textarea class="form-control" name="Sections[${secId}].Discussions[${idx}].Purpose" rows="2"></textarea>
                </div>
            </div>
        </div>`;
    cont.insertAdjacentHTML('beforeend', html);
}
function addTableToSection(secId){
    const cont = ensureSectionContainer(secId); if (!cont) return;
    const idx = cont.querySelectorAll('.component-item[data-type="table"]').length;
    const rowsId = `sec-${secId}-table-rows-${idx}`;
    const colsId = `sec-${secId}-table-cols-${idx}`;
    const previewId = `sec-${secId}-table-preview-${idx}`;
    const dataId = `sec-${secId}-table-data-${idx}`;
    const html = `
        <div class="card mb-3 component-item" data-type="table">
            <div class="card-header bg-warning bg-opacity-10 d-flex justify-content-between">
                <span><i class="fas fa-table me-2"></i>جدول</span>
                <button type="button" class="btn btn-sm btn-outline-danger" onclick="this.closest('.component-item').remove()"><i class="fas fa-trash"></i></button>
            </div>
            <div class="card-body">
                <div class="mb-2">
                    <label class="form-label">عنوان الجدول</label>
                    <input type="text" class="form-control" name="Sections[${secId}].Tables[${idx}].Title" required>
                </div>
                <div class="mb-2">
                    <label class="form-label">عدد الصفوف</label>
                    <input type="number" class="form-control" value="3" min="1" max="20" id="${rowsId}">
                </div>
                <div class="mb-2">
                    <label class="form-label">عدد الأعمدة</label>
                    <input type="number" class="form-control" value="3" min="1" max="10" id="${colsId}">
                </div>
                <button type="button" class="btn btn-primary" onclick="generateSectionTable(${secId}, ${idx})">
                    <i class="fas fa-table me-1"></i>إنشاء الجدول
                </button>
                <div id="${previewId}" class="mt-3"></div>
                <input type="hidden" id="${dataId}">
            </div>
        </div>`;
    cont.insertAdjacentHTML('beforeend', html);
}
function handleSectionImageUpload(secId, input){
    const cont = ensureSectionContainer(secId); if (!cont) return;
    const files = input.files; if (!files || files.length === 0) return;
    for (let file of files){
        const reader = new FileReader();
        reader.onload = function(e){
            const card = `
                <div class="card mb-3 component-item" data-type="image">
                    <div class="card-header bg-primary bg-opacity-10 d-flex justify-content-between">
                        <span><i class="fas fa-image me-2"></i>${file.name}</span>
                        <button type="button" class="btn btn-sm btn-outline-danger" onclick="this.closest('.component-item').remove()"><i class="fas fa-trash"></i></button>
                    </div>
                    <div class="card-body text-center">
                        <img src="${e.target.result}" class="img-fluid" style="max-height:200px">
                        <input type="hidden" value="${e.target.result}">
                    </div>
                </div>`;

            cont.insertAdjacentHTML('beforeend', card);
        };
        reader.readAsDataURL(file);
    }
    input.value = '';
}


// Add section-level Question
function addSectionQuestion(secId, surveyIdx){
    const cont = document.getElementById(`sec-${secId}-questions-${surveyIdx}`);
    if (!cont) return;
    if (cont.querySelector('.text-muted')) cont.innerHTML = '';
    const qId = ++sectionQuestionIdCounter;
    const html = `
        <div class="card mb-2" data-sec-question-id="${qId}">
            <div class="card-body">
                <div class="row g-2 mb-2">
                    <div class="col-md-8">
                        <input type="text" class="form-control" placeholder="نص السؤال" required>
                    </div>
                    <div class="col-md-3">
                        <select class="form-select">
                            <option value="0">اختيار واحد</option>
                            <option value="1">اختيار متعدد</option>
                        </select>
                    </div>
                    <div class="col-md-1">
                        <button type="button" class="btn btn-outline-danger w-100" onclick="this.closest('[data-sec-question-id]').remove()">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </div>
                <div class="options-container" id="sec-${secId}-options-${qId}">
                    <div class="input-group input-group-sm mb-1">
                        <span class="input-group-text">خيار 1</span>
                        <input type="text" class="form-control">
                    </div>
                    <div class="input-group input-group-sm mb-1">
                        <span class="input-group-text">خيار 2</span>
                        <input type="text" class="form-control">
                    </div>
                </div>
                <button type="button" class="btn btn-sm btn-outline-secondary" onclick="addSectionOption(${secId}, ${qId})">
                    <i class="fas fa-plus me-1"></i>خيار
                </button>
            </div>
        </div>`;
    cont.insertAdjacentHTML('beforeend', html);
}

function addSectionOption(secId, qId){
    const container = document.getElementById(`sec-${secId}-options-${qId}`);
    if (!container) return;
    const optionCount = container.querySelectorAll('.input-group').length;
    const html = `
        <div class="input-group input-group-sm mb-1">
            <span class="input-group-text">خيار ${optionCount + 1}</span>
            <input type="text" class="form-control">
            <button type="button" class="btn btn-outline-danger btn-sm" onclick="this.parentElement.remove()">
                <i class="fas fa-times"></i>
            </button>
        </div>`;
    container.insertAdjacentHTML('beforeend', html);
}

function generateSectionTable(secId, idx){
    const rows = parseInt(document.getElementById(`sec-${secId}-table-rows-${idx}`).value);
    const cols = parseInt(document.getElementById(`sec-${secId}-table-cols-${idx}`).value);
    const preview = document.getElementById(`sec-${secId}-table-preview-${idx}`);
    let tableHtml = '<table class="table table-bordered table-sm"><tbody>';
    const tableData = [];
    for (let i = 0; i < rows; i++){
        tableHtml += '<tr>';
        const row = [];
        for (let j = 0; j < cols; j++){
            tableHtml += '<td contenteditable="true" class="p-2" style="min-width:100px"></td>';
            row.push({ value: '' });
        }
        tableHtml += '</tr>';
        tableData.push(row);
    }
    tableHtml += '</tbody></table>';
    preview.innerHTML = tableHtml;
    const hidden = document.getElementById(`sec-${secId}-table-data-${idx}`);
    if (hidden) hidden.value = JSON.stringify({ rows: tableData });
}

function handleSectionPdfUpload(secId, input){
    const cont = ensureSectionContainer(secId); if (!cont) return;
    const files = input.files; if (!files || files.length === 0) return;
    for (let file of files){
        const reader = new FileReader();
        reader.onload = function(e){
            const card = `
                <div class="card mb-3 component-item" data-type="pdf">
                    <div class="card-header bg-danger bg-opacity-10 d-flex justify-content-between">
                        <span><i class="fas fa-file-pdf me-2"></i>${file.name}</span>
                        <button type="button" class="btn btn-sm btn-outline-danger" onclick="this.closest('.component-item').remove()"><i class="fas fa-trash"></i></button>
                    </div>
                    <div class="card-body">
                        <p class="mb-0"><i class="fas fa-file-pdf me-2 text-danger"></i>PDF</p>
                        <input type="hidden" value="${e.target.result}">
                    </div>
                </div>`;
            cont.insertAdjacentHTML('beforeend', card);
        };
        reader.readAsDataURL(file);
    }
    input.value = '';
}


// Check Empty Sections
function checkEmptySections() {
    const container = document.getElementById('sectionsContainer');
    if (!container.querySelector('.section-item')) {
        container.innerHTML = '<p class="text-muted text-center">لا توجد بنود. اضغط "إضافة بند" للبدء.</p>';
    }
}

