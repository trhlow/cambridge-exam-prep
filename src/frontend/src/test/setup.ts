import '@testing-library/jest-dom/vitest'

// jsdom has no pointer capture; the drag-and-drop cards call it on pointerdown.
Element.prototype.setPointerCapture ??= () => undefined
Element.prototype.releasePointerCapture ??= () => undefined

// jsdom's <dialog> has no showModal/close; the staff portal's modals open with them.
HTMLDialogElement.prototype.showModal ??= function (this: HTMLDialogElement) {
  this.setAttribute('open', '')
}
HTMLDialogElement.prototype.close ??= function (this: HTMLDialogElement) {
  this.removeAttribute('open')
}
