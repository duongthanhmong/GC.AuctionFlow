from gex.contracts import parse_symbol, product_from_root


def test_parse_comex_gold_option():
    c = parse_symbol("COMEX", "OGQ6 C4080")
    assert c is not None
    assert c.product == "GC" and c.is_call and c.strike == 4080.0
    p = parse_symbol("COMEX", "OGQ6 P4080")
    assert p.product == "GC" and not p.is_call and p.strike == 4080.0


def test_parse_cme_index_options():
    assert parse_symbol("CME", "E4AN6 C7435").product == "ES"
    assert parse_symbol("CME", "Q4AN6 P28200").product == "NQ"


def test_future_symbol_is_not_an_option():
    assert parse_symbol("COMEX", "GCQ6") is None


def test_product_from_root_fallbacks():
    assert product_from_root("COMEX", "OGQ6") == "GC"
    assert product_from_root("CME", "E4AN6") == "ES"
    assert product_from_root("CME", "Q5DN6") == "NQ"
    assert product_from_root("CME", "ZZZ") is None
