"""Gera docs/Documentação.pdf a partir de docs/Documentação.md, com a marca do Hermes.

O Markdown é a fonte: rode este script sempre que ele mudar, para os dois ficarem iguais.
Requer o ReportLab (pip install reportlab) e as fontes do Windows (Segoe UI, Bahnschrift, Consolas).

    python scripts/New-Documentation.py
"""
import html
import pathlib
import re
import xml.etree.ElementTree as ET

from reportlab import rl_config
from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (BaseDocTemplate, Frame, Image, KeepTogether, NextPageTemplate, PageBreak,
                                PageTemplate, Paragraph, Preformatted, Spacer, Table, TableStyle)
from reportlab.platypus.tableofcontents import TableOfContents

ROOT = pathlib.Path(__file__).resolve().parent.parent
SOURCE = ROOT / 'docs' / 'Documentação.md'
OUTPUT = ROOT / 'docs' / 'Documentação.pdf'
FONTS = pathlib.Path(r'C:\Windows\Fonts')

# PDF reprodutível: sem data de criação nem identificador aleatório.
rl_config.invariant = 1

# Paleta clara do Hermes (src/PrinterInstall.App/Themes/ThemeTokens.Light.xaml) e grafite da marca.
INK = colors.HexColor('#111A2C')
INK_2 = colors.HexColor('#465569')
INK_3 = colors.HexColor('#5F6E82')
ACCENT = colors.HexColor('#2F6FE0')
ACCENT_TEXT = colors.HexColor('#2155B3')
LINE = colors.HexColor('#DDE3EB')
SUNKEN = colors.HexColor('#F7F9FC')
CANVAS = colors.HexColor('#F1F4F8')
NOTE = colors.HexColor('#EBF2FD')
GRAPHITE = colors.HexColor('#101318')
GRAPHITE_TOP = colors.HexColor('#1F2530')
ON_GRAPHITE = colors.HexColor('#E6E9EE')
ON_GRAPHITE_2 = colors.HexColor('#98A2B0')
SUBTITLE = colors.HexColor('#8DB5FA')
WING = colors.HexColor('#E8A05C')
SHEET = colors.HexColor('#E6E9EE')
FOLD = colors.HexColor('#98A2B0')
CYAN, MAGENTA, YELLOW = colors.HexColor('#38BDF8'), colors.HexColor('#DB2777'), colors.HexColor('#EAB308')

PAGE_W, PAGE_H = A4
MARGIN_X = 22 * mm
MARGIN_TOP = 24 * mm
MARGIN_BOTTOM = 22 * mm
CONTENT_W = PAGE_W - 2 * MARGIN_X


def register_fonts():
    for name, file in [('Body', 'segoeui.ttf'), ('Body-Semibold', 'seguisb.ttf'), ('Body-Bold', 'segoeuib.ttf'),
                       ('Body-Italic', 'segoeuii.ttf'), ('Display', 'bahnschrift.ttf'), ('Mono', 'consola.ttf')]:
        pdfmetrics.registerFont(TTFont(name, str(FONTS / file)))
    pdfmetrics.registerFontFamily('Body', normal='Body', bold='Body-Semibold', italic='Body-Italic',
                                  boldItalic='Body-Semibold')


def read_version():
    props = ET.parse(ROOT / 'Directory.Build.props')
    return props.find('.//Version').text.strip()


STYLES = {}


def build_styles():
    base = dict(fontName='Body', fontSize=10, leading=15, textColor=INK)
    STYLES['body'] = ParagraphStyle('body', **base, spaceAfter=6)
    STYLES['h2'] = ParagraphStyle('h2', fontName='Body-Semibold', fontSize=19, leading=24, textColor=ACCENT,
                                  spaceBefore=2, spaceAfter=12)
    STYLES['h3'] = ParagraphStyle('h3', fontName='Body-Semibold', fontSize=12.5, leading=17, textColor=INK,
                                  spaceBefore=12, spaceAfter=5, keepWithNext=1)
    STYLES['bullet'] = ParagraphStyle('bullet', **base, leftIndent=14, bulletIndent=2, spaceAfter=3)
    STYLES['cell'] = ParagraphStyle('cell', fontName='Body', fontSize=8.8, leading=12, textColor=INK)
    STYLES['cell_head'] = ParagraphStyle('cell_head', fontName='Body-Semibold', fontSize=8.8, leading=12,
                                         textColor=INK_2)
    STYLES['code'] = ParagraphStyle('code', fontName='Mono', fontSize=8.5, leading=12, textColor=INK)
    STYLES['note'] = ParagraphStyle('note', **base)
    STYLES['caption'] = ParagraphStyle('caption', fontName='Body', fontSize=8.5, leading=12, textColor=INK_3,
                                       alignment=TA_CENTER, spaceBefore=4, spaceAfter=10)
    STYLES['intro'] = ParagraphStyle('intro', **{**base, 'textColor': INK_2}, spaceAfter=8)
    STYLES['toc_title'] = ParagraphStyle('toc_title', parent=STYLES['h2'])


# ---------------------------------------------------------------------------------------------------------------
# Emblema (mesmo desenho de Themes/Brand.xaml, em coordenadas 128 × 128 com origem no canto superior esquerdo)


def draw_emblem(c, x, y, size):
    c.saveState()
    c.translate(x, y + size)
    c.scale(size / 128, -size / 128)

    tile = c.beginPath()
    tile.roundRect(0, 0, 128, 128, 28)
    c.saveState()
    c.clipPath(tile, stroke=0, fill=0)
    c.linearGradient(0, 0, 0, 128, (GRAPHITE_TOP, GRAPHITE), extend=False)
    c.restoreState()

    c.setStrokeColor(colors.HexColor('#4C8DF6'))
    c.setStrokeAlpha(0.4)
    c.setLineWidth(3)
    c.roundRect(1.5, 1.5, 125, 125, 26.5, stroke=1, fill=0)
    c.setStrokeAlpha(1)

    sheet = c.beginPath()
    sheet.moveTo(56, 28)
    sheet.lineTo(86, 28)
    sheet.lineTo(98, 40)
    sheet.lineTo(98, 100)
    sheet.lineTo(56, 100)
    sheet.close()
    c.setFillColor(SHEET)
    c.drawPath(sheet, stroke=0, fill=1)
    fold = c.beginPath()
    fold.moveTo(86, 28)
    fold.lineTo(86, 40)
    fold.lineTo(98, 40)
    fold.close()
    c.setFillColor(FOLD)
    c.drawPath(fold, stroke=0, fill=1)

    c.setLineCap(1)
    c.setLineWidth(5)
    for color, (x1, y1, x2) in [(CYAN, (65, 54, 89)), (MAGENTA, (65, 66, 89)), (YELLOW, (65, 78, 81))]:
        c.setStrokeColor(color)
        c.line(x1, y1, x2, y1)

    c.setStrokeColor(WING)
    c.setLineWidth(6)
    for start, c1, c2, end in [((56, 50), (44, 48), (30, 40), (20, 28)),
                               ((56, 62), (44, 61), (32, 56), (22, 48)),
                               ((56, 74), (46, 74), (38, 71), (30, 66))]:
        feather = c.beginPath()
        feather.moveTo(*start)
        feather.curveTo(*c1, *c2, *end)
        c.drawPath(feather, stroke=1, fill=0)
    c.restoreState()


# ---------------------------------------------------------------------------------------------------------------
# Páginas


class Manual(BaseDocTemplate):
    def __init__(self, path, version):
        super().__init__(str(path), pagesize=A4, leftMargin=MARGIN_X, rightMargin=MARGIN_X, topMargin=MARGIN_TOP,
                         bottomMargin=MARGIN_BOTTOM, title='Hermes · Manual do usuário', author='Hermes',
                         subject=f'Guia de uso do Hermes {version}', creator='scripts/New-Documentation.py')
        self.version = version
        frame = Frame(MARGIN_X, MARGIN_BOTTOM, CONTENT_W, PAGE_H - MARGIN_TOP - MARGIN_BOTTOM, id='body',
                      leftPadding=0, rightPadding=0, topPadding=0, bottomPadding=0)
        self.addPageTemplates([PageTemplate('cover', frames=[frame], onPage=self.draw_cover),
                               PageTemplate('page', frames=[frame], onPage=self.draw_page)])
        self._heading = 0

    def beforeDocument(self):
        # O sumário só se estabiliza se as chaves forem as mesmas em cada passagem do multiBuild.
        self._heading = 0

    def afterFlowable(self, flowable):
        level = getattr(flowable, 'toc_level', None)
        if level is None:
            return
        text = flowable.getPlainText()
        self._heading += 1
        key = f'h{self._heading}'
        self.canv.bookmarkPage(key)
        self.canv.addOutlineEntry(text, key, level=level, closed=level > 0)
        if level == 0:
            self.notify('TOCEntry', (0, text, self.page, key))

    def draw_cover(self, c, _doc):
        band = PAGE_H * 0.48
        c.saveState()
        c.setFillColor(GRAPHITE)
        c.rect(0, PAGE_H - band, PAGE_W, band, stroke=0, fill=1)
        top = PAGE_H - 40 * mm
        draw_emblem(c, MARGIN_X, top - 24 * mm, 24 * mm)
        c.setFillColor(ON_GRAPHITE)
        c.setFont('Display', 46)
        c.drawString(MARGIN_X - 1, top - 48 * mm, 'HERMES', charSpace=15)
        c.setFillColor(SUBTITLE)
        c.setFont('Mono', 9.5)
        c.drawString(MARGIN_X + 1, top - 57 * mm, 'IMPLANTAÇÃO DE IMPRESSORAS', charSpace=3.4)
        c.setFillColor(ON_GRAPHITE)
        c.setFont('Body', 17)
        c.drawString(MARGIN_X, top - 76 * mm, 'Manual do usuário')
        c.setFillColor(ON_GRAPHITE_2)
        c.setFont('Body', 10)
        c.drawString(MARGIN_X, top - 84 * mm,
                     'Instalação e controle de impressoras de rede e etiquetadoras em estações Windows')

        c.setFillColor(INK_2)
        c.setFont('Body', 9.5)
        for i, line in enumerate([f'Versão do aplicativo: {self.version}',
                                  'Público: técnicos de suporte, helpdesk e analistas de campo',
                                  'Os nomes, domínios e endereços deste manual são fictícios.']):
            c.drawString(MARGIN_X, 34 * mm - i * 6 * mm, line)
        c.restoreState()

    def draw_page(self, c, doc):
        c.saveState()
        c.setStrokeColor(LINE)
        c.setLineWidth(0.6)
        c.line(MARGIN_X, PAGE_H - 14 * mm, PAGE_W - MARGIN_X, PAGE_H - 14 * mm)
        c.line(MARGIN_X, 14 * mm, PAGE_W - MARGIN_X, 14 * mm)
        c.setFont('Body', 8)
        c.setFillColor(INK_3)
        c.drawString(MARGIN_X, 9.5 * mm, f'Hermes {self.version} · Manual do usuário')
        c.drawRightString(PAGE_W - MARGIN_X, 9.5 * mm, f'Página {doc.page}')
        c.restoreState()


class Heading(Paragraph):
    def __init__(self, text, style, toc_level):
        super().__init__(text, style)
        self.toc_level = toc_level


# ---------------------------------------------------------------------------------------------------------------
# Markdown → flowables (só o subconjunto usado em docs/Documentação.md)


def inline(text):
    """Converte negrito, itálico, código e links para a marcação de parágrafo do ReportLab."""
    parts = re.split(r'(`[^`]+`)', text)
    out = []
    for part in parts:
        if part.startswith('`') and part.endswith('`') and len(part) > 1:
            out.append(f'<font name="Mono" size="9" color="#253247">{html.escape(part[1:-1])}</font>')
            continue
        part = html.escape(part, quote=False)
        part = re.sub(r'\*\*(.+?)\*\*', r'<b>\1</b>', part)
        part = re.sub(r'(?<![\w*])\*(?!\s)(.+?)(?<!\s)\*(?![\w*])', r'<i>\1</i>', part)

        def link(m):
            label, target = m.group(1), m.group(2)
            if target.startswith('#') or target.lower().endswith('.pdf'):
                return label
            return f'<a href="{html.escape(target)}" color="#2155B3">{label}</a>'

        part = re.sub(r'\[([^\]]+)\]\(([^)]+)\)', link, part)
        out.append(part)
    return ''.join(out)


def heading_text(text):
    return html.escape(text.strip())


def table(rows):
    header, body = rows[0], rows[2:]
    data = [[Paragraph(inline(c), STYLES['cell_head']) for c in header]]
    data += [[Paragraph(inline(c), STYLES['cell']) for c in row] for row in body]
    widths = column_widths(header, body)
    t = Table(data, colWidths=widths, repeatRows=1, hAlign='LEFT')
    t.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, 0), CANVAS),
        ('LINEBELOW', (0, 0), (-1, 0), 0.8, LINE),
        ('LINEBELOW', (0, 1), (-1, -1), 0.5, LINE),
        ('BOX', (0, 0), (-1, -1), 0.6, LINE),
        ('VALIGN', (0, 0), (-1, -1), 'TOP'),
        ('LEFTPADDING', (0, 0), (-1, -1), 6),
        ('RIGHTPADDING', (0, 0), (-1, -1), 6),
        ('TOPPADDING', (0, 0), (-1, -1), 4.5),
        ('BOTTOMPADDING', (0, 0), (-1, -1), 4.5),
    ]))
    return t


def column_widths(header, body):
    # Largura proporcional ao texto mais longo de cada coluna, sem ficar mais estreita que a
    # palavra mais longa dela (senão o ReportLab quebra a palavra no meio).
    padding = 12
    lengths, minimums = [], []
    for i in range(len(header)):
        cells = [re.sub(r'[`*]', '', c) for c in [header[i]] + [row[i] for row in body if i < len(row)]]
        lengths.append(max(8, min(60, max(len(c) for c in cells))))
        words = [w for c in cells for w in c.split()]
        minimums.append(max(pdfmetrics.stringWidth(w, 'Body-Semibold', 8.8) for w in words) + padding)
    total = sum(lengths)
    widths = [max(CONTENT_W * n / total, minimum) for n, minimum in zip(lengths, minimums)]
    # O que as colunas mínimas ganharam sai, por igual, das que tinham folga.
    excess = sum(widths) - CONTENT_W
    flexible = [i for i, w in enumerate(widths) if w > minimums[i]]
    for i in flexible:
        widths[i] -= excess / len(flexible)
    return widths


def figure(src, alt, width_pct, number):
    path = (SOURCE.parent / src).resolve()
    img = Image(str(path))
    width = CONTENT_W * width_pct / 100
    img.drawWidth, img.drawHeight = width, img.imageHeight * width / img.imageWidth
    frame = Table([[img]], colWidths=[width + 2], hAlign='CENTER')
    frame.setStyle(TableStyle([('BOX', (0, 0), (-1, -1), 0.6, LINE),
                               ('LEFTPADDING', (0, 0), (-1, -1), 1), ('RIGHTPADDING', (0, 0), (-1, -1), 1),
                               ('TOPPADDING', (0, 0), (-1, -1), 1), ('BOTTOMPADDING', (0, 0), (-1, -1), 1)]))
    return KeepTogether([frame, Paragraph(f'Figura {number}. {html.escape(alt)}.', STYLES['caption'])])


def code_style(code):
    # Linha longa demais para a caixa: a fonte diminui até caber, em vez de vazar pela borda.
    available = CONTENT_W - 20
    base = STYLES['code']
    widest = max(pdfmetrics.stringWidth(line, base.fontName, base.fontSize) for line in code)
    if widest <= available:
        return base
    size = max(6.5, base.fontSize * available / widest)
    return ParagraphStyle('code_fit', parent=base, fontSize=size, leading=size * 1.4)


def shaded(flowable, background, border=None):
    box = Table([[flowable]], colWidths=[CONTENT_W])
    style = [('BACKGROUND', (0, 0), (-1, -1), background),
             ('LEFTPADDING', (0, 0), (-1, -1), 10), ('RIGHTPADDING', (0, 0), (-1, -1), 10),
             ('TOPPADDING', (0, 0), (-1, -1), 8), ('BOTTOMPADDING', (0, 0), (-1, -1), 8)]
    if border is not None:
        style.append(('BOX', (0, 0), (-1, -1), 0.6, border))
    box.setStyle(TableStyle(style))
    return box


LINE_BREAK = '\u2028'


def parse(markdown):
    lines = markdown.splitlines()
    story, intro = [], []
    paragraph, figures = [], 0
    in_body = False
    i = 0

    def flush():
        nonlocal paragraph
        if paragraph:
            # Dois espaços no fim de uma linha do Markdown viram quebra de linha.
            text = ' '.join(line.rstrip() + (LINE_BREAK if line.endswith('  ') else '') for line in paragraph)
            text = inline(text).replace(LINE_BREAK + ' ', '<br/>').replace(LINE_BREAK, '<br/>')
            (story if in_body else intro).append(Paragraph(text, STYLES['body' if in_body else 'intro']))
            paragraph = []

    while i < len(lines):
        line = lines[i]
        stripped = line.strip()

        if stripped.startswith('# '):
            i += 1
            continue
        if stripped == '## Sumário':
            # O sumário do Markdown vira o sumário com páginas, gerado pelo ReportLab.
            i += 1
            while i < len(lines) and not lines[i].startswith('---'):
                i += 1
            continue
        if stripped.startswith('## '):
            flush()
            if in_body:
                story.append(PageBreak())
            in_body = True
            story.append(Heading(heading_text(stripped[3:]), STYLES['h2'], 0))
            i += 1
            continue
        if stripped.startswith('### '):
            flush()
            story.append(Heading(heading_text(stripped[4:]), STYLES['h3'], 1))
            i += 1
            continue
        if stripped == '---' or not stripped:
            flush()
            i += 1
            continue
        if stripped.startswith('Também disponível em [PDF]'):
            i += 1
            continue
        if stripped.startswith('```'):
            flush()
            code = []
            i += 1
            while i < len(lines) and not lines[i].strip().startswith('```'):
                code.append(lines[i])
                i += 1
            i += 1
            story.append(shaded(Preformatted('\n'.join(code), code_style(code)), SUNKEN, LINE))
            story.append(Spacer(1, 8))
            continue
        if stripped.startswith('|'):
            flush()
            rows = []
            while i < len(lines) and lines[i].strip().startswith('|'):
                rows.append([cell.strip() for cell in lines[i].strip().strip('|').split('|')])
                i += 1
            story.append(table(rows))
            story.append(Spacer(1, 10))
            continue
        if stripped.startswith('>'):
            flush()
            quote = []
            while i < len(lines) and lines[i].strip().startswith('>'):
                quote.append(lines[i].strip().lstrip('>').strip())
                i += 1
            note = shaded(Paragraph(inline(' '.join(quote)), STYLES['note']), NOTE)
            # O aviso fica na página do parágrafo que comenta, e o título da seção vai junto.
            previous = story.pop() if story and isinstance(story[-1], Paragraph) else None
            title = story.pop() if previous is not None and story and isinstance(story[-1], Heading) else None
            story.append(KeepTogether([f for f in (title, previous, Spacer(1, 4), note) if f is not None]))
            story.append(Spacer(1, 10))
            continue
        image = re.search(r'<img src="([^"]+)" alt="([^"]+)" width="(\d+)%"', stripped)
        if image:
            flush()
            figures += 1
            story.append(figure(image.group(1), image.group(2), int(image.group(3)), figures))
            i += 1
            continue
        bullet = re.match(r'[-*] (.*)', stripped)
        number = re.match(r'(\d+)\. (.*)', stripped)
        if bullet or number:
            flush()
            text = bullet.group(1) if bullet else number.group(2)
            marker = '•' if bullet else f'{number.group(1)}.'
            story.append(Paragraph(inline(text), STYLES['bullet'], bulletText=marker))
            i += 1
            continue

        paragraph.append(line)
        i += 1

    flush()
    return intro, story


def build():
    register_fonts()
    build_styles()
    version = read_version()
    intro, body = parse(SOURCE.read_text(encoding='utf-8-sig'))

    toc = TableOfContents()
    toc.levelStyles = [ParagraphStyle('toc0', fontName='Body', fontSize=11, leading=20, textColor=INK,
                                      leftIndent=0, firstLineIndent=0)]
    toc.dotsMinLevel = 0

    story = [NextPageTemplate('page'), PageBreak()]
    story += intro
    story += [Spacer(1, 10), Paragraph('Sumário', STYLES['toc_title']), toc, PageBreak()]
    story += body

    doc = Manual(OUTPUT, version)
    doc.multiBuild(story)
    print(f'{OUTPUT.relative_to(ROOT)} gerado ({version}).')


if __name__ == '__main__':
    build()
