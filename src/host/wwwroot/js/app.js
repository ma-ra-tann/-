// --- API Client ---
const api = {
    async getVCList() {
        const res = await fetch('/api/vc');
        return res.json();
    },
    async getVCDetail(vcName) {
        const res = await fetch(`/api/vc/${encodeURIComponent(vcName)}`);
        return res.json();
    },
    async getCapitalistDetail(vcName, capitalistName) {
        const res = await fetch(`/api/capitalist/${encodeURIComponent(vcName)}/${encodeURIComponent(capitalistName)}`);
        return res.json();
    },
    async analyzeVC(vcName) {
        const res = await fetch(`/api/vc/${encodeURIComponent(vcName)}/analyze`, { method: 'POST' });
        return res.json();
    },
    async addVC(name, stage, theme) {
        const res = await fetch('/api/vc/add', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name, investmentStage: stage, investmentTheme: theme })
        });
        return res.json();
    },
    async importCSV(file) {
        const form = new FormData();
        form.append('file', file);
        const res = await fetch('/api/vc/import-csv', { method: 'POST', body: form });
        return res.json();
    }
};

// --- State ---
let currentVC = null;

// --- View Routing ---
function showView(viewId) {
    document.querySelectorAll('.view').forEach(v => v.classList.remove('active'));
    document.getElementById(viewId).classList.add('active');
}

// --- Screen 1: VC List ---
async function loadVCList() {
    showView('view-vc-list');
    currentVC = null;
    const funds = await api.getVCList();
    const tbody = document.getElementById('vc-table-body');
    const empty = document.getElementById('vc-empty');
    const table = document.getElementById('vc-table');

    if (funds.length === 0) {
        table.style.display = 'none';
        empty.style.display = 'block';
        return;
    }

    table.style.display = 'table';
    empty.style.display = 'none';
    tbody.innerHTML = funds.map(f => {
        const hasUrl = f.websiteUrl && f.websiteUrl.trim() !== '' && f.websiteUrl !== '調査不足（URL不明）' && f.websiteUrl !== '調査不足（明記なし）';
        const safeUrl = hasUrl ? f.websiteUrl.replace(/"/g, '&quot;') : '';
        return `
        <tr>
            <td>
                <div style="display: flex; align-items: center; gap: 0.5rem;">
                    <a href="#" class="vc-link" data-name="${escapeHtml(f.name)}">${escapeHtml(f.name)}</a>
                    ${hasUrl ? `
                    <a href="${safeUrl}" target="_blank" class="url-btn" title="公式サイトを開く">
                        <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"></path><polyline points="15 3 21 3 21 9"></polyline><line x1="10" y1="14" x2="21" y2="3"></line></svg>
                    </a>
                    ` : ''}
                </div>
            </td>
            <td>${escapeHtml(f.investmentStage)}</td>
            <td>${escapeHtml(f.investmentTheme)}</td>
            <td>${f.analysisStatus === '未分析'
                ? '<span class="status-badge unknown">未分析</span>'
                : escapeHtml(f.analysisStatus)}</td>
            <td>
                ${f.analysisStatus === '未分析'
                    ? `<button class="btn btn-analyze" data-name="${escapeHtml(f.name)}">分析</button>`
                    : `<button class="btn btn-detail" data-name="${escapeHtml(f.name)}">詳細</button>`}
            </td>
        </tr>
    `}).join('');

    // Event: VC name link
    tbody.querySelectorAll('.vc-link').forEach(link => {
        link.addEventListener('click', e => {
            e.preventDefault();
            loadVCDetail(link.dataset.name);
        });
    });

    // Event: Analyze All button
    const btnAnalyzeAll = document.getElementById('btn-analyze-all');
    if (btnAnalyzeAll) {
        // 未分析のVCがあるかチェックしてボタンの有効/無効を切り替え
        const hasUnanalyzed = funds.some(f => f.analysisStatus === '未分析');
        btnAnalyzeAll.disabled = !hasUnanalyzed;
        
        // 既存のイベントリスナーが重複しないように一度クローンして置き換える
        const newBtnAnalyzeAll = btnAnalyzeAll.cloneNode(true);
        btnAnalyzeAll.parentNode.replaceChild(newBtnAnalyzeAll, btnAnalyzeAll);
        
        newBtnAnalyzeAll.addEventListener('click', async () => {
            const unanalyzedFunds = funds.filter(f => f.analysisStatus === '未分析');
            if (unanalyzedFunds.length === 0) return;
            
            if (!confirm(`未分析のVC ${unanalyzedFunds.length}件 を一括分析します。よろしいですか？\n（※APIの呼び出しに時間がかかる場合があります）`)) {
                return;
            }
            
            newBtnAnalyzeAll.disabled = true;
            newBtnAnalyzeAll.textContent = '一括分析中...';
            newBtnAnalyzeAll.classList.add('analyzing');
            
            // 順番に分析を実行
            for (const fund of unanalyzedFunds) {
                try {
                    // UI上の該当行のボタンも「分析中」にする
                    const rowBtn = document.querySelector(`.btn-analyze[data-name="${escapeHtml(fund.name)}"]`);
                    if (rowBtn) {
                        rowBtn.disabled = true;
                        rowBtn.textContent = '分析中...';
                        rowBtn.classList.add('analyzing');
                    }
                    
                    await api.analyzeVC(fund.name);
                } catch (e) {
                    console.error(`Failed to analyze ${fund.name}:`, e);
                }
            }
            
            newBtnAnalyzeAll.textContent = '未分析を一括分析';
            newBtnAnalyzeAll.classList.remove('analyzing');
            await loadVCList();
        });
    }

    // Event: Analyze button
    tbody.querySelectorAll('.btn-analyze').forEach(btn => {
        btn.addEventListener('click', async () => {
            btn.disabled = true;
            btn.textContent = '分析中...';
            btn.classList.add('analyzing');
            try {
                await api.analyzeVC(btn.dataset.name);
                await loadVCList();
            } catch {
                btn.textContent = 'エラー';
                btn.disabled = false;
            }
        });
    });

    // Event: Detail button
    tbody.querySelectorAll('.btn-detail').forEach(btn => {
        btn.addEventListener('click', () => loadVCDetail(btn.dataset.name));
    });
}

// --- Screen 2: VC Detail ---
async function loadVCDetail(vcName) {
    showView('view-vc-detail');
    currentVC = vcName;
    const detail = await api.getVCDetail(vcName);

    const hasUrl = detail.websiteUrl && detail.websiteUrl.trim() !== '' && detail.websiteUrl !== '調査不足（URL不明）' && detail.websiteUrl !== '調査不足（明記なし）';
    const safeUrl = hasUrl ? detail.websiteUrl.replace(/"/g, '&quot;') : '';

    document.getElementById('vc-detail-header').innerHTML = `
        <div class="vc-info">
            <div style="display: flex; align-items: center; gap: 0.75rem;">
                <h2>${escapeHtml(detail.name)}</h2>
                ${hasUrl ? `
                <a href="${safeUrl}" target="_blank" class="url-btn" title="公式サイトを開く" style="margin-top: 4px;">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"></path><polyline points="15 3 21 3 21 9"></polyline><line x1="10" y1="14" x2="21" y2="3"></line></svg>
                </a>
                ` : ''}
            </div>
            <p class="vc-meta">
                <span style="display: inline-flex; align-items: center; gap: 4px;">
                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21.21 15.89A10 10 0 1 1 8 2.83"></path><path d="M22 12A10 10 0 0 0 12 2v10z"></path></svg>
                    ${escapeHtml(detail.investmentStage)}
                </span>
                <span style="margin: 0 0.5rem; color: #cbd5e1;">|</span>
                <span style="display: inline-flex; align-items: center; gap: 4px;">
                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z"></path><line x1="7" y1="7" x2="7.01" y2="7"></line></svg>
                    ${escapeHtml(detail.investmentTheme)}
                </span>
            </p>
        </div>
    `;

    const list = document.getElementById('capitalist-list');
    if (detail.capitalists.length === 0) {
        list.innerHTML = '<p class="empty-message">キャピタリストがまだ分析されていません。</p>';
        return;
    }

    list.innerHTML = detail.capitalists.map(c => `
        <div class="capitalist-card ${statusClass(c.interestStatus)}"
             data-name="${escapeHtml(c.name)}" data-vc="${escapeHtml(vcName)}">
            <div class="name">
                <span class="status-badge ${statusClass(c.interestStatus)}">${statusLabel(c.interestStatus)}</span>
                ${escapeHtml(c.name)}
            </div>
            <div class="meta">${escapeHtml(c.title)} | ${escapeHtml(c.investmentDomain)}</div>
            ${c.evidenceSummary ? `<div class="summary">「${escapeHtml(c.evidenceSummary)}」</div>` : ''}
        </div>
    `).join('');

    list.querySelectorAll('.capitalist-card').forEach(card => {
        card.addEventListener('click', () => {
            loadCapitalistDetail(card.dataset.vc, card.dataset.name);
        });
    });
}

// --- Screen 3: Capitalist Detail ---
async function loadCapitalistDetail(vcName, capitalistName) {
    showView('view-capitalist-detail');
    const detail = await api.getCapitalistDetail(vcName, capitalistName);

    document.getElementById('btn-back-to-vc').onclick = () => loadVCDetail(vcName);

    document.getElementById('capitalist-detail-content').innerHTML = `
        <div class="vc-info">
            <h2 style="display: flex; align-items: center; gap: 0.5rem;">
                <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="color: var(--text-muted);"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>
                ${escapeHtml(detail.name)}
            </h2>
            <p class="vc-meta" style="margin-top: 0.5rem;">
                <span style="font-weight: 500; color: var(--text-main);">${escapeHtml(detail.title)}</span>
                <span style="color: #cbd5e1;">@</span> 
                <span>${escapeHtml(vcName)}</span>
            </p>
            <p class="vc-meta" style="margin-top: 0.25rem;">
                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="2" y1="12" x2="22" y2="12"></line><path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"></path></svg>
                投資担当: ${escapeHtml(detail.investmentDomain)}
            </p>
        </div>

        <div class="judgment-box">
            <h3>
                <span class="status-badge ${statusClass(detail.interestStatus)}" style="font-size: 0.85rem; padding: 0.35rem 0.75rem;">
                    ${statusLabel(detail.interestStatus)}
                </span>
                財務モデリングへの関心判定
            </h3>
        </div>

        <h3 style="margin: 2rem 0 1rem; font-size: 1.1rem; color: var(--text-main); display: flex; align-items: center; gap: 0.5rem;">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline><line x1="16" y1="13" x2="8" y2="13"></line><line x1="16" y1="17" x2="8" y2="17"></line><polyline points="10 9 9 9 8 9"></polyline></svg>
            判定の根拠（エビデンス）
        </h3>
        <div class="evidence-list">
            ${detail.evidences.length === 0
                ? '<div style="padding: 3rem; text-align: center; color: var(--text-muted);">根拠となる情報は見つかりませんでした</div>'
                : detail.evidences.map(e => `
                    <div class="evidence-item">
                        <span class="evidence-type">${escapeHtml(typeLabel(e.type))}</span>
                        <div style="margin-top: 0.5rem; color: var(--text-main); font-size: 0.95rem;">${escapeHtml(e.summary)}</div>
                        ${e.sourceUrl && e.sourceUrl !== '調査不足（URL不明）' ? `
                            <a href="${escapeHtml(e.sourceUrl)}" target="_blank" class="evidence-url">
                                <svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"></path><polyline points="15 3 21 3 21 9"></polyline><line x1="10" y1="14" x2="21" y2="3"></line></svg>
                                情報ソースを確認
                            </a>
                        ` : ''}
                    </div>
                `).join('')}
        </div>
    `;
}

// --- Modal ---
const modal = document.getElementById('modal-import');
document.getElementById('btn-import').addEventListener('click', () => modal.classList.add('active'));
document.getElementById('modal-close').addEventListener('click', () => modal.classList.remove('active'));
modal.addEventListener('click', e => { if (e.target === modal) modal.classList.remove('active'); });

// Tabs
document.querySelectorAll('.tab-btn').forEach(btn => {
    btn.addEventListener('click', () => {
        document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
        document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
        btn.classList.add('active');
        document.getElementById(`tab-${btn.dataset.tab}`).classList.add('active');
    });
});

// CSV import
const csvFile = document.getElementById('csv-file');
const csvBtn = document.getElementById('btn-csv-import');
csvFile.addEventListener('change', () => {
    const file = csvFile.files[0];
    if (file) {
        document.getElementById('csv-preview').textContent = `選択: ${file.name}`;
        csvBtn.disabled = false;
    }
});
csvBtn.addEventListener('click', async () => {
    csvBtn.disabled = true;
    csvBtn.textContent = '取り込み中...';
    const result = await api.importCSV(csvFile.files[0]);
    modal.classList.remove('active');
    csvBtn.textContent = '取り込む';
    csvFile.value = '';
    document.getElementById('csv-preview').textContent = '';
    
    // Show a toast or notification that background analysis has started
    const toast = document.createElement('div');
    toast.style.cssText = 'position:fixed;bottom:20px;right:20px;background:#4CAF50;color:white;padding:15px;border-radius:4px;z-index:1000;box-shadow:0 2px 5px rgba(0,0,0,0.2);';
    toast.textContent = `${result.length}件のVCを取り込み、バックグラウンドで分析を開始しました。リストは自動的に更新されませんが、定期的にリロードしてください。`;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 5000);

    await loadVCList();
});

// Manual add
document.getElementById('btn-manual-add').addEventListener('click', async () => {
    const name = document.getElementById('manual-name').value.trim();
    const stage = document.getElementById('manual-stage').value;
    const theme = document.getElementById('manual-theme').value.trim();
    if (!name) return;
    await api.addVC(name, stage, theme);
    document.getElementById('manual-name').value = '';
    document.getElementById('manual-theme').value = '';
    modal.classList.remove('active');
    await loadVCList();
});

// Search filter
document.getElementById('search-input').addEventListener('input', e => {
    const query = e.target.value.toLowerCase();
    document.querySelectorAll('#vc-table-body tr').forEach(row => {
        const name = row.querySelector('td').textContent.toLowerCase();
        row.style.display = name.includes(query) ? '' : 'none';
    });
});

// Navigation
document.getElementById('btn-back-to-list').addEventListener('click', () => loadVCList());

// --- Helpers ---
function escapeHtml(str) {
    if (!str || str === '調査不足（明記なし）' || str === '調査不足（URL不明）') return '<span style="color: #ccc;">-</span>';
    return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

function statusClass(status) {
    switch (status) {
        case 'Interested': return 'interested';
        case 'NotInterested': return 'notinterested';
        default: return 'unknown';
    }
}

function statusLabel(status) {
    switch (status) {
        case 'Interested': return '⚪ 関心あり';
        case 'NotInterested': return '✖ 関心なし';
        default: return '△ 不明';
    }
}

function typeLabel(type) {
    switch (type) {
        case 'OfficialProfile': return '公式プロフィール';
        case 'Background': return '経歴・職歴';
        case 'SocialMedia': return 'SNS発信';
        case 'Blog': return 'ブログ・note';
        case 'Podcast': return '音声・動画';
        case 'Article': return '記事・インタビュー';
        case 'Talk': return '登壇・イベント';
        case 'Portfolio': return '投資実績';
        case 'InvestmentThesis': return '投資方針';
        case 'Statement': return 'その他の発言';
        case 'Other': return 'その他';
        default: return type;
    }
}

// --- Init ---
loadVCList();
