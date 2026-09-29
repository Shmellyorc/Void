(() => {
  const KEYWORDS = new Set([
    'abstract','add','and','as','base','break','case','catch','class','const','continue','default','delegate','do','else','enum','event','explicit','extern','finally','for','foreach','get','if','implicit','in','interface','internal','is','namespace','new','not','operator','or','out','override','params','private','protected','public','readonly','ref','remove','return','sealed','set','sizeof','stackalloc','static','struct','switch','this','throw','try','typeof','unsafe','using','var','virtual','when','where','while','yield'
  ]);
  const BUILTIN_TYPES = new Set([
    'bool','byte','char','decimal','double','float','int','long','nint','nuint','object','sbyte','short','string','uint','ulong','ushort','void'
  ]);
  const LITERALS = new Set(['true','false','null']);

  const appendToken = (fragment, text, className = '') => {
    if (!text) return;
    if (!className) {
      fragment.appendChild(document.createTextNode(text));
      return;
    }
    const span = document.createElement('span');
    span.className = className;
    span.textContent = text;
    fragment.appendChild(span);
  };

  const highlightCode = (element, source) => {
    const fragment = document.createDocumentFragment();
    let i = 0;

    while (i < source.length) {
      const ch = source[i];
      const next = source[i + 1];

      if (ch === '/' && next === '/') {
        const end = source.indexOf('\n', i);
        const stop = end === -1 ? source.length : end;
        appendToken(fragment, source.slice(i, stop), 'syntax-comment');
        i = stop;
        continue;
      }

      if (ch === '/' && next === '*') {
        const end = source.indexOf('*/', i + 2);
        const stop = end === -1 ? source.length : end + 2;
        appendToken(fragment, source.slice(i, stop), 'syntax-comment');
        i = stop;
        continue;
      }

      if (ch === '"' || ch === "'") {
        const quote = ch;
        let end = i + 1;
        while (end < source.length) {
          if (source[end] === '\\') {
            end += 2;
            continue;
          }
          if (source[end] === quote) {
            end++;
            break;
          }
          end++;
        }
        appendToken(fragment, source.slice(i, end), 'syntax-string');
        i = end;
        continue;
      }

      if (ch === '#') {
        const match = source.slice(i).match(/^#[A-Za-z_][A-Za-z0-9_]*/);
        if (match) {
          appendToken(fragment, match[0], 'syntax-preprocessor');
          i += match[0].length;
          continue;
        }
      }

      if (/\d/.test(ch)) {
        const match = source.slice(i).match(/^(?:0[xX][0-9a-fA-F]+|0[bB][01]+|\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)(?:[fFdDmMuUlL]{0,2})/);
        if (match) {
          appendToken(fragment, match[0], 'syntax-number');
          i += match[0].length;
          continue;
        }
      }

      if (/[A-Za-z_]/.test(ch)) {
        const match = source.slice(i).match(/^[A-Za-z_][A-Za-z0-9_]*/);
        if (match) {
          const word = match[0];
          let className = '';
          if (KEYWORDS.has(word)) className = 'syntax-keyword';
          else if (BUILTIN_TYPES.has(word)) className = 'syntax-type';
          else if (LITERALS.has(word)) className = 'syntax-literal';
          else if (/^[A-Z]/.test(word)) className = 'syntax-symbol';
          else {
            const after = source.slice(i + word.length).match(/^\s*/)?.[0]?.length ?? 0;
            if (source[i + word.length + after] === '(') className = 'syntax-function';
          }
          appendToken(fragment, word, className);
          i += word.length;
          continue;
        }
      }

      if ('+-*/%=!<>&|^~?:'.includes(ch)) {
        let op = ch;
        if (next && '=>=<=!=++--&&||???.::+=-=*=/=%=&=|=^=<<>>'.includes(ch + next)) {
          op += next;
          i++;
          if ((op === '??' || op === '?.') && source[i + 1] === '=') {
            op += '=';
            i++;
          }
        }
        appendToken(fragment, op, 'syntax-operator');
        i++;
        continue;
      }

      appendToken(fragment, ch);
      i++;
    }

    element.replaceChildren(fragment);
  };

  const highlightStaticCode = () => {
    document.querySelectorAll('[data-void-code]').forEach((element) => {
      highlightCode(element, element.textContent || '');
    });
  };

  window.VOID_SYNTAX = { highlightCode };

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', highlightStaticCode, { once: true });
  } else {
    highlightStaticCode();
  }
})();
