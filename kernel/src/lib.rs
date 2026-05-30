//! GravyBox native kernel — C-ABI surface consumed by C# via P/Invoke.
//!
//! This is the first, deliberately-minimal chokepoint across the C# <-> Rust
//! seam: produce a string in Rust and hand ownership to the C# caller. The real
//! kernel (CSG bake / manifold solidification) grows behind this same boundary.
//!
//! Ownership rule for every string this module hands out: **Rust allocates, the
//! caller frees** by returning the pointer to [`gbox_string_free`]. Never free a
//! kernel string with the C# allocator, and never free it twice.

use std::ffi::CString;
use std::os::raw::c_char;

/// Return the kernel's greeting as a newly-allocated, NUL-terminated UTF-8
/// string. Ownership transfers to the caller, who **must** release it with
/// [`gbox_string_free`].
///
/// Returns a non-null pointer on success.
#[no_mangle]
pub extern "C" fn gbox_hello() -> *mut c_char {
    // The literal contains no interior NUL, so this never fails.
    CString::new("hello from the GravyBox Rust kernel")
        .expect("greeting literal contains no interior NUL byte")
        .into_raw()
}

/// Free a string previously produced by this kernel (e.g. [`gbox_hello`]).
///
/// Passing a null pointer is a safe no-op. Passing any pointer not produced by
/// this kernel, or freeing the same pointer twice, is undefined behavior.
///
/// # Safety
/// `s` must be either null or a pointer returned by a kernel function that
/// documents [`gbox_string_free`] as its release routine, and must not have been
/// freed already.
#[no_mangle]
pub unsafe extern "C" fn gbox_string_free(s: *mut c_char) {
    if s.is_null() {
        return;
    }
    // SAFETY: by contract `s` was produced by `CString::into_raw` in this module;
    // reconstituting and dropping it releases exactly that allocation once. The
    // enclosing fn is `unsafe`, so this body is already an unsafe context.
    drop(CString::from_raw(s));
}
