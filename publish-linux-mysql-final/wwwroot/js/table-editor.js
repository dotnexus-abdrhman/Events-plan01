(function(){
  'use strict';

  const MAX_PASTE_ROWS = 50;
  const MAX_PASTE_COLS = 20;
  const MAX_JSON_BYTES = 200 * 1024; // 200KB

  function $(sel, root){ return (root||document).querySelector(sel); }
  function $all(sel, root){ return Array.from((root||document).querySelectorAll(sel)); }

  function sanitizeHtml(html){
    // Allow only: b,i,u,strong,em,br and inline style text-align
    const container = document.createElement('div');
    container.innerHTML = html;
    const allowed = new Set(['B','I','U','STRONG','EM','BR']);

    (function walk(node){
      const kids = Array.from(node.childNodes);
      for(const k of kids){
        if(k.nodeType === 1){ // element
          const tag = k.tagName;
          if(!allowed.has(tag)){
            // unwrap element but keep children/text
            while(k.firstChild) node.insertBefore(k.firstChild, k);
            node.removeChild(k);
            continue;
          }
          // Attributes: allow only style with text-align
          for(const attr of Array.from(k.attributes)){
            if(attr.name.toLowerCase() === 'style'){
              const ta = (k.style.textAlign || '').trim();
              k.removeAttribute('style');
              if(ta) k.style.textAlign = ta;
            } else {
              k.removeAttribute(attr.name);
            }
          }
          walk(k);
        } else if(k.nodeType === 8){ // comment
          node.removeChild(k);
        }
      }
    })(container);

    return container.innerHTML;
  }

  function parseClipboard(text){
    // Tabs -> columns, newlines -> rows
    const rows = text.split(/\r?\n/).filter(r=>r.length>0).map(r=> r.split('\t'));
    return rows;
  }

  function ensureLimit(rows){
    if(rows.length > MAX_PASTE_ROWS) return false;
    if(rows.some(r=> r.length > MAX_PASTE_COLS)) return false;
    return true;
  }

  function buildEmpty(rows, cols){
    return Array.from({length: rows}, ()=> Array.from({length: cols}, ()=> ''));
  }

  function bytes(str){ return new TextEncoder().encode(str).length; }

  function createToolbar(){
    const wrap = document.createElement('div');
    wrap.className = 'te-toolbar d-flex flex-wrap gap-1 mb-2';
    wrap.innerHTML = [
      '<div class="btn-group btn-group-sm" role="group">',
      '  <button type="button" class="btn btn-outline-secondary" data-cmd="bold" title="غامق"><b>B</b></button>',
      '  <button type="button" class="btn btn-outline-secondary" data-cmd="italic" title="مائل"><i>I</i></button>',
      '  <button type="button" class="btn btn-outline-secondary" data-cmd="underline" title="تحته خط"><u>U</u></button>',
      '</div>',
      '<div class="btn-group btn-group-sm ms-2" role="group">',
      '  <button type="button" class="btn btn-outline-secondary" data-align="right" title="محاذاة يمين">يمين</button>',
      '  <button type="button" class="btn btn-outline-secondary" data-align="center" title="وسط">وسط</button>',
      '  <button type="button" class="btn btn-outline-secondary" data-align="left" title="يسار">يسار</button>',
      '</div>',
      '<div class="btn-group btn-group-sm ms-2" role="group">',
      '  <button type="button" class="btn btn-outline-secondary" data-merge title="دمج">دمج</button>',
      '  <button type="button" class="btn btn-outline-secondary" data-split title="فك الدمج">فك</button>',
      '</div>',
      '<div class="btn-group btn-group-sm ms-2" role="group">',
      '  <button type="button" class="btn btn-outline-secondary" data-add-row="above" title="صف أعلى">+ صف ↑</button>',
      '  <button type="button" class="btn btn-outline-secondary" data-add-row="below" title="صف أسفل">+ صف ↓</button>',
      '  <button type="button" class="btn btn-outline-secondary" data-del-row title="حذف صف">− صف</button>',
      '</div>',
      '<div class="btn-group btn-group-sm ms-2" role="group">',
      '  <button type="button" class="btn btn-outline-secondary" data-add-col="left" title="عمود يمين">+ عمود →</button>',
      '  <button type="button" class="btn btn-outline-secondary" data-add-col="right" title="عمود يسار">+ عمود ←</button>',
      '  <button type="button" class="btn btn-outline-secondary" data-del-col title="حذف عمود">− عمود</button>',
      '</div>',
      '<div class="btn-group btn-group-sm ms-2" role="group">',
      '  <button type="button" class="btn btn-outline-secondary" data-clear title="مسح تنسيق">مسح</button>',
      '</div>',
      '<div class="btn-group btn-group-sm ms-2" role="group">',
      '  <button type="button" class="btn btn-outline-danger" data-delete-table title="حذف الجدول">حذف الجدول</button>',
      '</div>'
    ].join('');
    return wrap;
  }

  function buildModelFromJson(jsonStr){
    try{
      const data = JSON.parse(jsonStr||'null');
      if(!data) return null;
      if(Array.isArray(data.rows)){
        return {
          title: data.title||'',
          description: data.description||'',
          hasHeader: !!data.hasHeader,
          rows: data.rows.map(r=> r.map(c=> String(c||'')))
        };
      }
      // Backward compatibility: columns/rows
      if(Array.isArray(data.columns) && Array.isArray(data.rows)){
        const head = [data.columns.map(c=> String((c.name||'')||''))];
        return { title:'', description:'', hasHeader:true, rows: head.concat(data.rows.map(r=> r.map(c=> String(c||'')))) };
      }
    }catch{}
    return null;
  }

  function buildDefault(rows=3, cols=3, hasHeader=true){
    const grid = buildEmpty(rows, cols);
    return { title:'', description:'', hasHeader: !!hasHeader, rows: grid };
  }

  function renderTable(state, tableEl){
    // state.rows: array of arrays of HTML strings
    tableEl.innerHTML = '';
    const thead = document.createElement('thead');
    const tbody = document.createElement('tbody');

    if(state.hasHeader){
      const tr = document.createElement('tr');
      const row = state.rows[0] || [];
      row.forEach((cell, cIdx)=>{
        const th = document.createElement('th');
        th.className = 'te-cell';
        th.dataset.r = '0';
        th.dataset.c = String(cIdx);
        const inner = document.createElement('div');
        inner.className = 'te-inner';
        inner.contentEditable = 'true';
        inner.dir = 'rtl';
        inner.dataset.r = '0';
        inner.dataset.c = String(cIdx);
        inner.innerHTML = cell;
        th.appendChild(inner);
        addResizer(th);
        tr.appendChild(th);
      });
      thead.appendChild(tr);
    }

    const startRow = state.hasHeader ? 1 : 0;
    for(let r=startRow; r<state.rows.length; r++){
      const tr = document.createElement('tr');
      const row = state.rows[r] || [];
      row.forEach((cell, cIdx)=>{
        const td = document.createElement('td');
        td.className = 'te-cell';
        td.dataset.r = String(r);
        td.dataset.c = String(cIdx);
        const inner = document.createElement('div');
        inner.className = 'te-inner';
        inner.contentEditable = 'true';
        inner.dir = 'rtl';
        inner.dataset.r = String(r);
        inner.dataset.c = String(cIdx);
        inner.innerHTML = cell;
        td.appendChild(inner);
        tr.appendChild(td);
      });
      tbody.appendChild(tr);
    }

    tableEl.appendChild(thead);
    tableEl.appendChild(tbody);
  }

  function addResizer(th){
    const res = document.createElement('span');
    res.className = 'te-col-resizer';
    th.style.position = 'relative';
    res.style.position = 'absolute';
    res.style.left = '0';
    res.style.top = '0';
    res.style.width = '6px';
    res.style.cursor = 'col-resize';
    res.style.userSelect = 'none';
    res.style.height = '100%';
    th.appendChild(res);

    let startX=0, startW=0;
    function onMove(e){
      const dx = (e.touches? e.touches[0].clientX : e.clientX) - startX;
      const w = Math.max(60, startW + dx);
      th.style.width = w + 'px';
    }
    function onUp(){
      document.removeEventListener('mousemove', onMove);
      document.removeEventListener('mouseup', onUp);
      document.removeEventListener('touchmove', onMove);
      document.removeEventListener('touchend', onUp);
    }
    function onDown(e){
      startX = (e.touches? e.touches[0].clientX : e.clientX);
      startW = th.getBoundingClientRect().width;
      document.addEventListener('mousemove', onMove);
      document.addEventListener('mouseup', onUp);
      document.addEventListener('touchmove', onMove, {passive:false});
      document.addEventListener('touchend', onUp);
      e.preventDefault();
      e.stopPropagation();
    }
    res.addEventListener('mousedown', onDown);
    res.addEventListener('touchstart', onDown, {passive:false});
  }

  function getActiveCell(container){
    const sel = window.getSelection();
    if(!sel || sel.rangeCount===0) return null;
    const node = sel.anchorNode && (sel.anchorNode.nodeType===1? sel.anchorNode : sel.anchorNode.parentElement);
    if(!node) return null;
    const cell = node.closest('.te-inner') || node.closest('.te-cell');
    return container.contains(cell) ? cell : null;
  }

  function saveToHidden(container){
    const wrap = container.closest('.table-item');
    const input = wrap && wrap.querySelector('input[name$=".Json"]');
    if(!input) return;
    const title = wrap.querySelector('input[name$=".Title"]')?.value || '';
    const description = wrap.querySelector('input[name$=".Description"]')?.value || '';
    const hasHeader = container._state.hasHeader;
    const rows = [];
    const table = container.querySelector('table');
    const thead = table.querySelector('thead');
    const tbody = table.querySelector('tbody');

    if(hasHeader && thead){
      const hr = [];
      $all('th .te-inner', thead).forEach(el=> hr.push(sanitizeHtml(el.innerHTML)));
      rows.push(hr);
    }
    $all('tr', tbody).forEach(tr=>{
      const rr = [];
      $all('td .te-inner', tr).forEach(el=> rr.push(sanitizeHtml(el.innerHTML)));
      rows.push(rr);
    });

    const json = JSON.stringify({ title, description, hasHeader, rows });
    if(bytes(json) > MAX_JSON_BYTES){
      // Show gentle warning but still set the input to avoid data loss
      if(!container._sizeWarned){
        alert('حجم الجدول كبير جدًا (أكثر من 200KB). يُرجى تقليل المحتوى.');
        container._sizeWarned = true;
      }
    }
    input.value = json;
  }

  function applyInlineCmd(cell, cmd){
    const target = cell.classList?.contains('te-inner') ? cell : (cell.querySelector?.('.te-inner') || cell);
    target.focus();
    document.execCommand(cmd,false,null);
  }

  function setAlign(cell, dir){
    const target = cell.classList?.contains('te-inner') ? cell : (cell.querySelector?.('.te-inner') || cell);
    target.style.textAlign = dir;
  }

  function addRow(container, where){
    const table = container.querySelector('table');
    const rows = container._state.rows;
    const cols = rows[0] ? rows[0].length : 0;
    const active = getActiveCell(container);
    const r = active ? parseInt(active.dataset.r,10) : (container._state.hasHeader?1:0);
    const idx = where==='above' ? r : r+1;
    rows.splice(idx, 0, Array.from({length: cols}, ()=>''));
    renderTable(container._state, table);
  }

  function addCol(container, side){
    const table = container.querySelector('table');
    const rows = container._state.rows;
    const active = getActiveCell(container);
    const c = active ? parseInt(active.dataset.c,10) : 0;
    const idx = side==='left' ? c : c+1;
    for(const row of rows){ row.splice(idx,0,''); }
    renderTable(container._state, table);
  }

  function delRow(container){
    const rows = container._state.rows;
    if(rows.length <= (container._state.hasHeader?1:1)){ alert('لا يمكن حذف جميع الصفوف.'); return; }
    const active = getActiveCell(container);
    const r = active ? parseInt(active.dataset.r,10) : (container._state.hasHeader?1:0);
    if(container._state.hasHeader && r===0){ alert('لا تحذف صف العنوان.'); return; }
    rows.splice(r,1);
    renderTable(container._state, container.querySelector('table'));
  }

  function delCol(container){
    const rows = container._state.rows;
    if(!rows[0] || rows[0].length<=1){ alert('لا يمكن حذف كل الأعمدة.'); return; }
    const active = getActiveCell(container);
    const c = active ? parseInt(active.dataset.c,10) : 0;
    for(const row of rows){ row.splice(c,1); }
    renderTable(container._state, container.querySelector('table'));
  }

  function mergeSelected(container){
    // Simple: merge current cell with the cell to the right (2 cells) as MVP
    const cell = getActiveCell(container);
    if(!cell) return;
    const r = parseInt(cell.dataset.r,10), c = parseInt(cell.dataset.c,10);
    const rows = container._state.rows;
    if(rows[r] && rows[r][c+1] !== undefined){
      const html = sanitizeHtml(cell.innerHTML) + ' ' + sanitizeHtml(container.querySelector(`[data-r="${r}"][data-c="${c+1}"]`).innerHTML);
      rows[r][c] = html;
      rows[r].splice(c+1,1);
      renderTable(container._state, container.querySelector('table'));
    }
  }

  function splitCell(container){
    // Split current cell horizontally into two
    const cell = getActiveCell(container);
    if(!cell) return;
    const r = parseInt(cell.dataset.r,10), c = parseInt(cell.dataset.c,10);
    container._state.rows[r].splice(c+1,0,'');
    renderTable(container._state, container.querySelector('table'));
  }

  function attachHandlers(container){
    const table = container.querySelector('table');
    const toolbar = container.querySelector('.te-toolbar');
    const headerToggle = container.querySelector('input.te-hasHeader');

    // Toolbar actions
    if(toolbar){
      toolbar.addEventListener('click', function(e){
        const btn = e.target.closest('button'); if(!btn) return;
        const cell = getActiveCell(container);
        if(btn.dataset.cmd && cell){ applyInlineCmd(cell, btn.dataset.cmd); saveToHidden(container); }
        if(btn.dataset.align && cell){ setAlign(cell, btn.dataset.align); saveToHidden(container); }
        if(btn.hasAttribute('data-merge')){ mergeSelected(container); saveToHidden(container); }
        if(btn.hasAttribute('data-split')){ splitCell(container); saveToHidden(container); }
        if(btn.hasAttribute('data-add-row')){ addRow(container, btn.getAttribute('data-add-row')); saveToHidden(container); }
        if(btn.hasAttribute('data-del-row')){ delRow(container); saveToHidden(container); }
        if(btn.hasAttribute('data-add-col')){ addCol(container, btn.getAttribute('data-add-col')); saveToHidden(container); }
        if(btn.hasAttribute('data-del-col')){ delCol(container); saveToHidden(container); }
        if(btn.hasAttribute('data-clear') && cell){ cell.innerHTML = sanitizeHtml(cell.textContent||''); saveToHidden(container); }
        if(btn.hasAttribute('data-delete-table')){
          const card = container.closest('.table-item');
          if(card){ card.remove(); }
        }
      });
    }

    if(headerToggle){
      headerToggle.addEventListener('change', function(){
        const old = container._state.hasHeader;
        const want = headerToggle.checked;
        if(want===old) return;
        container._state.hasHeader = want;
        renderTable(container._state, table);
        saveToHidden(container);
      });
    }

    // typing/paste
    table.addEventListener('input', function(){ saveToHidden(container); });

    table.addEventListener('keydown', function(e){
      if(e.key === 'Tab'){
        // allow browser to move focus; we just save
        setTimeout(()=> saveToHidden(container), 0);
      } else if(e.key === 'Enter' && (e.ctrlKey||e.metaKey)){
        addRow(container, 'below');
        saveToHidden(container);
        e.preventDefault();
      }
    });

    table.addEventListener('paste', function(e){
      const cell = getActiveCell(container);
      if(!cell) return;
      const txt = (e.clipboardData && e.clipboardData.getData('text/plain')) || '';
      if(!txt || (!txt.includes('\t') && !/\r?\n/.test(txt))) return; // default paste
      e.preventDefault();
      const grid = parseClipboard(txt);
      if(!ensureLimit(grid)){
        alert('المحتوى الملصوق كبير جدًا (حتى 50×20 كحد أقصى).');
        return;
      }
      const startR = parseInt(cell.dataset.r,10);
      const startC = parseInt(cell.dataset.c,10);
      // Ensure grid can fit
      const needRows = startR + grid.length - container._state.rows.length + 1;
      for(let i=0;i<needRows;i++) container._state.rows.push(Array.from({length: container._state.rows[0].length}, ()=>''));
      const needCols = Math.max(0, ...grid.map(r=> r.length)) - (container._state.rows[0].length - startC);
      if(needCols>0){ for(const row of container._state.rows){ for(let i=0;i<needCols;i++) row.push(''); } }
      // Put values
      for(let r=0;r<grid.length;r++){
        for(let c=0;c<grid[r].length;c++){
          const R = startR + r, C = startC + c;
          container._state.rows[R][C] = sanitizeHtml(grid[r][c]);
        }
      }
      renderTable(container._state, container.querySelector('table'));
      saveToHidden(container);
    });

    // Long-press on mobile: show simple action hint
    let lpTimer=null; let lpTarget=null;
    table.addEventListener('touchstart', function(e){
      lpTarget = e.target.closest('.te-inner, .te-cell');
      lpTimer = setTimeout(()=>{
        if(lpTarget){ lpTarget.focus(); alert('خيارات: دمج/فك، إضافة/حذف صف/عمود من الشريط العلوي.'); }
      }, 700);
    }, {passive:true});
    table.addEventListener('touchend', function(){ if(lpTimer){ clearTimeout(lpTimer); lpTimer=null; } }, {passive:true});
  }

  function init(container){
    if(container._inited) return; container._inited = true;
    // Ensure toolbar UI is present
    const toolbarHost = container.querySelector('.te-toolbar');
    if(toolbarHost){
      const tb = createToolbar();
      toolbarHost.replaceWith(tb);
    }
    const table = container.querySelector('table');
    const hasHeaderInp = container.querySelector('input.te-hasHeader');
    const jsonInput = container.closest('.table-item')?.querySelector('input[name$=".Json"]');
    let state = null;
    if(jsonInput && jsonInput.value){ state = buildModelFromJson(jsonInput.value); }
    if(!state) state = buildDefault(3,3,true);
    container._state = state;
    if(hasHeaderInp) hasHeaderInp.checked = !!state.hasHeader;
    renderTable(state, table);
    attachHandlers(container);
    saveToHidden(container);
  }

  function attachFormGuard(form){
    form.addEventListener('submit', function(e){
      let stop = false; // نمنع الإرسال فقط عند تجاوز الحجم الكبير
      $all('#tables-wrapper .table-item').forEach(function(card){
        const input = card.querySelector('input[name$=".Json"]');
        let json = input.value || '';
        if(!json) return; // جدول فارغ اختياري
        try{
          const data = JSON.parse(json);
          const rows = Array.isArray(data.rows)? data.rows : [];
          // ضمان وجود صف وعمود على الأقل بدون منع الإرسال
          if(rows.length < 1){ rows.push(['']); }
          if(!rows[0] || rows[0].length < 1){ rows[0] = ['']; }
          // إن كان الهيدر مفعلاً وأول خلية فارغة، نملؤها افتراضيًا بدل المنع
          if(data.hasHeader){
            const firstTxt = rows[0][0] ? rows[0][0].replace(/<[^>]*>/g,'').trim() : '';
            if(!firstTxt){ rows[0][0] = rows[0][0] || 'عنوان'; }
          }
          const newJson = JSON.stringify({
            title: data.title || '',
            description: data.description || '',
            hasHeader: !!data.hasHeader,
            rows
          });
          input.value = newJson; // تحديث الحقل المخفي بالقيمة المصححة
          if(bytes(newJson) > MAX_JSON_BYTES){ stop = true; }
        }catch{ /* لا نمنع الإرسال بسبب JSON غير متوقع */ }
      });
      if(stop){ e.preventDefault(); alert('حجم أحد الجداول كبير جدًا (الحد 200KB). قلل التنسيق أو المحتوى ثم أعد المحاولة.'); }
    });
  }

  // Export to global
  window.TableEditor = { init, attachFormGuard };
})();

